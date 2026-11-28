import 'dart:async';

import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:image_picker/image_picker.dart';

/// Infinite list over a paged endpoint (`{ items, page, pageSize, totalCount }`): first page on open, next page
/// near the end, pull to refresh, and the shared loading / empty / error states (UX §6). Give it a new [key] when
/// its filters change so it starts again from page 1.
class PagedListView<T> extends StatefulWidget {
  const PagedListView({
    super.key,
    required this.fetch,
    required this.itemBuilder,
    required this.empty,
    this.header,
    this.padding = const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 24),
    this.separator = 8,
  });

  final Future<PagedResult<T>> Function(int page) fetch;
  final Widget Function(BuildContext context, T item) itemBuilder;
  final Widget empty;

  /// Shown above the items (e.g. totals); scrolls with the list.
  final Widget? header;
  final EdgeInsets padding;
  final double separator;

  @override
  State<PagedListView<T>> createState() => PagedListViewState<T>();
}

class PagedListViewState<T> extends State<PagedListView<T>> {
  final List<T> _items = [];
  int _page = 0;
  bool _hasMore = true;
  bool _loading = false;
  Object? _error;

  @override
  void initState() {
    super.initState();
    unawaited(_loadMore());
  }

  /// Starts again from page 1 (after an edit, or pull to refresh).
  Future<void> reload() async {
    setState(() {
      _items.clear();
      _page = 0;
      _hasMore = true;
      _error = null;
    });
    await _loadMore();
  }

  Future<void> _loadMore() async {
    if (_loading || !_hasMore) {
      return;
    }
    setState(() => _loading = true);
    try {
      final result = await widget.fetch(_page + 1);
      if (!mounted) {
        return;
      }
      setState(() {
        _items.addAll(result.items);
        _page = result.page;
        _hasMore = result.hasMore;
        _error = null;
      });
    } on Object catch (error) {
      if (mounted) {
        setState(() => _error = error);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_items.isEmpty) {
      final Widget body;
      if (_error != null) {
        body = ErrorView(error: _error!, onRetry: reload);
      } else if (_loading || _hasMore) {
        body = const SkeletonList();
      } else {
        body = widget.empty;
      }
      return RefreshIndicator(
        onRefresh: reload,
        child: ListView(
          padding: widget.padding,
          children: [?widget.header, body],
        ),
      );
    }
    final headerCount = widget.header == null ? 0 : 1;
    final footerCount = (_hasMore || _error != null) ? 1 : 0;
    return RefreshIndicator(
      onRefresh: reload,
      child: NotificationListener<ScrollNotification>(
        onNotification: (notification) {
          if (notification.metrics.extentAfter < 600) {
            unawaited(_loadMore());
          }
          return false;
        },
        child: ListView.separated(
          padding: widget.padding,
          itemCount: headerCount + _items.length + footerCount,
          separatorBuilder: (_, _) => SizedBox(height: widget.separator),
          itemBuilder: (context, index) {
            if (index < headerCount) {
              return widget.header!;
            }
            final i = index - headerCount;
            if (i < _items.length) {
              return widget.itemBuilder(context, _items[i]);
            }
            if (_error != null) {
              return ErrorView(error: _error!, onRetry: _loadMore, compact: true);
            }
            return const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            );
          },
        ),
      ),
    );
  }
}

/// Search box that reports the text after the user pauses typing.
class SearchField extends StatefulWidget {
  const SearchField({super.key, required this.hint, required this.onChanged, this.initial = ''});

  final String hint;
  final String initial;
  final ValueChanged<String> onChanged;

  @override
  State<SearchField> createState() => _SearchFieldState();
}

class _SearchFieldState extends State<SearchField> {
  late final TextEditingController _controller = TextEditingController(text: widget.initial);
  Timer? _debounce;

  @override
  void dispose() {
    _debounce?.cancel();
    _controller.dispose();
    super.dispose();
  }

  void _changed(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), () => widget.onChanged(value.trim()));
    setState(() {});
  }

  @override
  Widget build(BuildContext context) => TextField(
    controller: _controller,
    onChanged: _changed,
    textInputAction: TextInputAction.search,
    decoration: InputDecoration(
      hintText: widget.hint,
      prefixIcon: const Icon(Icons.search_rounded),
      suffixIcon: _controller.text.isEmpty
          ? null
          : IconButton(
              tooltip: 'Clear',
              icon: const Icon(Icons.close_rounded),
              onPressed: () {
                _controller.clear();
                _changed('');
              },
            ),
    ),
  );
}

/// One-line row of filter chips (single choice); `null` value = "All".
class ChoiceChips<T> extends StatelessWidget {
  const ChoiceChips({super.key, required this.options, required this.selected, required this.onSelected});

  final Map<T?, String> options;
  final T? selected;
  final ValueChanged<T?> onSelected;

  @override
  Widget build(BuildContext context) => SingleChildScrollView(
    scrollDirection: Axis.horizontal,
    child: Row(
      children: [
        for (final option in options.entries) ...[
          ChoiceChip(
            label: Text(option.value),
            selected: option.key == selected,
            showCheckmark: false,
            onSelected: (_) => onSelected(option.key),
          ),
          const SizedBox(width: 8),
        ],
      ],
    ),
  );
}

/// Big number with a label, for the dashboard and report totals.
class KpiTile extends StatelessWidget {
  const KpiTile({super.key, required this.label, required this.value, required this.icon, this.tone = Tone.ok, this.onTap});

  final String label;
  final String value;
  final IconData icon;
  final Tone tone;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => AppCard(
    onTap: onTap,
    padding: const EdgeInsets.all(14),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        IconTile(icon, tone: tone, size: 36),
        const SizedBox(height: 10),
        Text(value, style: dmStyle(24, FontWeight.w800, color: context.dm.text, tabular: true)),
        Text(label, style: context.text.bodySmall, maxLines: 2),
      ],
    ),
  );
}

/// Simple vertical bar chart (no chart package): one bar per value, labels under every [labelEvery]th bar.
class BarChart extends StatelessWidget {
  const BarChart({super.key, required this.values, required this.labels, this.height = 140, this.labelEvery = 3});

  final List<int> values;
  final List<String> labels;
  final double height;
  final int labelEvery;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    final max = values.fold<int>(0, (a, b) => a > b ? a : b);
    return Semantics(
      label: 'Bar chart: ${[for (var i = 0; i < values.length; i++) if (values[i] > 0) '${labels[i]} ${values[i]}'].join(', ')}',
      child: SizedBox(
        height: height + 20,
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            for (var i = 0; i < values.length; i++)
              Expanded(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    Tooltip(
                      message: '${labels[i]}: ${values[i]}',
                      child: Container(
                        margin: const EdgeInsets.symmetric(horizontal: 1.5),
                        height: max == 0 ? 2 : (values[i] / max * height).clamp(2, height),
                        decoration: BoxDecoration(
                          color: values[i] == 0 ? c.sunken : c.primary,
                          borderRadius: const BorderRadius.vertical(top: Radius.circular(3)),
                        ),
                      ),
                    ),
                    SizedBox(
                      height: 20,
                      child: i % labelEvery == 0
                          ? FittedBox(child: Text(labels[i], style: context.text.labelSmall))
                          : null,
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}

/// GST date range (both days inclusive), picked with the platform date range picker.
class DateRange {
  const DateRange(this.from, this.to);

  /// The last [days] days up to today (GST).
  factory DateRange.lastDays(int days) {
    final today = Gst.today();
    return DateRange(today.subtract(Duration(days: days - 1)), today);
  }

  factory DateRange.thisMonth() {
    final today = Gst.today();
    return DateRange(DateTime(today.year, today.month), today);
  }

  final DateTime from;
  final DateTime to;

  String get label {
    String d(DateTime x) => '${x.day} ${const ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'][x.month - 1]}';
    return from == to ? '${d(from)} ${from.year}' : '${d(from)} – ${d(to)} ${to.year}';
  }

  @override
  bool operator ==(Object other) => other is DateRange && other.from == from && other.to == to;

  @override
  int get hashCode => Object.hash(from, to);
}

/// Button showing a date range; tapping opens the picker (max [maxDays] days, the reports' limit).
class DateRangeButton extends StatelessWidget {
  const DateRangeButton({super.key, required this.range, required this.onChanged, this.maxDays = 366});

  final DateRange range;
  final ValueChanged<DateRange> onChanged;
  final int maxDays;

  Future<void> _pick(BuildContext context) async {
    final today = Gst.today();
    final picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(today.year - 5),
      lastDate: today,
      initialDateRange: DateTimeRange(start: range.from, end: range.to),
      helpText: 'Pick dates (UAE time)',
    );
    if (picked == null) {
      return;
    }
    final days = picked.end.difference(picked.start).inDays + 1;
    if (days > maxDays && context.mounted) {
      showMessage(context, 'Pick at most $maxDays days.');
      return;
    }
    onChanged(DateRange(picked.start, picked.end));
  }

  @override
  Widget build(BuildContext context) => OutlinedButton.icon(
    // The theme makes buttons full width; this one sits in rows next to titles and filters.
    style: OutlinedButton.styleFrom(minimumSize: const Size(0, 44), padding: const EdgeInsets.symmetric(horizontal: 14)),
    onPressed: () => _pick(context),
    icon: const Icon(Icons.date_range_rounded),
    label: Text(range.label),
  );
}

/// Shows why a save failed. A stale record (409 CONCURRENCY_CONFLICT) gets a clear "reload" message; field
/// errors stay under their inputs; anything else is a snack bar with the server's reference.
void showSaveError(BuildContext context, ApiException error, {VoidCallback? onReload}) {
  if (error.isConflict) {
    showDialog<void>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Changed by someone else'),
        content: const Text('Someone saved this record after you opened it. Reload it to see their changes, then try again.'),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.of(dialogContext).pop();
              onReload?.call();
            },
            child: const Text('Reload'),
          ),
        ],
      ),
    );
    return;
  }
  if (error.fieldErrors.isEmpty) {
    showError(context, error);
  }
}

/// Text field section with a title, for long forms.
class FormSection extends StatelessWidget {
  const FormSection({super.key, required this.title, required this.children});

  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      SectionLabel(title),
      AppCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            for (final (i, child) in children.indexed) ...[if (i > 0) const SizedBox(height: 14), child],
          ],
        ),
      ),
    ],
  );
}

/// Whole-number input (capacities, counts, minutes).
class NumberField extends StatelessWidget {
  const NumberField({
    super.key,
    required this.controller,
    required this.label,
    this.errorText,
    this.helperText,
    this.min = 0,
    this.max = 100000,
  });

  final TextEditingController controller;
  final String label;
  final String? errorText;
  final String? helperText;
  final int min;
  final int max;

  @override
  Widget build(BuildContext context) => TextFormField(
    controller: controller,
    keyboardType: TextInputType.number,
    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
    decoration: InputDecoration(labelText: label, errorText: errorText, helperText: helperText, helperMaxLines: 3),
    validator: (value) {
      final number = int.tryParse(value ?? '');
      if (number == null) {
        return 'Enter a number.';
      }
      return number < min || number > max ? 'Use $min to $max.' : null;
    },
  );
}

/// Takes a photo or picks one from the gallery, resized on the phone (longest side 1,600 px, JPEG) before upload
/// (D-057). Returns null when the user cancels.
Future<List<int>?> pickPhoto(BuildContext context) async {
  final source = await showModalBottomSheet<ImageSource>(
    context: context,
    builder: (context) => SafeArea(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ListTile(
            leading: const Icon(Icons.photo_camera_rounded),
            title: const Text('Take a photo'),
            onTap: () => Navigator.of(context).pop(ImageSource.camera),
          ),
          ListTile(
            leading: const Icon(Icons.photo_library_rounded),
            title: const Text('Choose from gallery'),
            onTap: () => Navigator.of(context).pop(ImageSource.gallery),
          ),
        ],
      ),
    ),
  );
  if (source == null) {
    return null;
  }
  final file = await ImagePicker().pickImage(source: source, maxWidth: 1600, maxHeight: 1600, imageQuality: 85);
  return file?.readAsBytes();
}

/// Photo with a camera button: shows the current photo (or [placeholder]) and uploads a new one through [upload].
class PhotoEditor extends StatefulWidget {
  const PhotoEditor({super.key, required this.photoUrl, required this.upload, required this.placeholder, this.size = 120});

  final String? photoUrl;
  final Future<void> Function(List<int> jpegBytes) upload;
  final Widget placeholder;
  final double size;

  @override
  State<PhotoEditor> createState() => _PhotoEditorState();
}

class _PhotoEditorState extends State<PhotoEditor> {
  bool _busy = false;

  Future<void> _change() async {
    final bytes = await pickPhoto(context);
    if (bytes == null) {
      return;
    }
    setState(() => _busy = true);
    try {
      await widget.upload(bytes);
      if (mounted) {
        showMessage(context, 'Photo saved.');
      }
    } on ApiException catch (error) {
      if (mounted) {
        showError(context, error);
      }
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    return Stack(
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(DmSize.rCard),
          child: SizedBox(
            width: widget.size,
            height: widget.size,
            child: widget.photoUrl == null
                ? ColoredBox(color: c.surface2, child: Center(child: widget.placeholder))
                : AuthImage(widget.photoUrl!, width: widget.size, height: widget.size),
          ),
        ),
        Positioned(
          right: 6,
          bottom: 6,
          child: Material(
            color: c.primary,
            shape: const CircleBorder(),
            child: InkWell(
              customBorder: const CircleBorder(),
              onTap: _busy ? null : _change,
              child: Padding(
                padding: const EdgeInsets.all(8),
                child: _busy
                    ? SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: c.onPrimary))
                    : Icon(Icons.photo_camera_rounded, size: 18, color: c.onPrimary),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

/// Generic list row: leading icon or avatar, title, subtitle lines, trailing widget, chevron when tappable.
class ListRow extends StatelessWidget {
  const ListRow({super.key, required this.title, this.leading, this.subtitle, this.trailing, this.onTap, this.flat = false});

  final Widget? leading;
  final String title;
  final String? subtitle;
  final Widget? trailing;
  final VoidCallback? onTap;
  final bool flat;

  @override
  Widget build(BuildContext context) => AppCard(
    onTap: onTap,
    flat: flat,
    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
    child: Row(
      children: [
        if (leading != null) ...[leading!, const SizedBox(width: 12)],
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: context.text.titleSmall, maxLines: 1, overflow: TextOverflow.ellipsis),
              if (subtitle != null) Text(subtitle!, style: context.text.bodySmall, maxLines: 2, overflow: TextOverflow.ellipsis),
            ],
          ),
        ),
        if (trailing != null) ...[const SizedBox(width: 8), trailing!],
        if (onTap != null) Icon(Icons.chevron_right_rounded, color: context.dm.text3),
      ],
    ),
  );
}

/// "Active" / "Inactive" badge for master data and accounts.
Widget activeBadge(bool isActive) => isActive
    ? const StatusBadge('Active', tone: Tone.ok, icon: Icons.check_circle_rounded)
    : const StatusBadge('Inactive', tone: Tone.neutral, icon: Icons.block_rounded);

/// Asks for one line of text (names of categories, types, areas, locations). Returns null when cancelled.
Future<String?> askText(BuildContext context, {required String title, required String label, String initial = '', String action = 'Save'}) {
  final controller = TextEditingController(text: initial);
  final form = GlobalKey<FormState>();
  return showDialog<String>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text(title),
      content: Form(
        key: form,
        child: TextFormField(
          controller: controller,
          autofocus: true,
          textCapitalization: TextCapitalization.words,
          decoration: InputDecoration(labelText: label),
          validator: (value) => (value ?? '').trim().isEmpty ? 'Enter a name.' : null,
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Cancel')),
        TextButton(
          onPressed: () {
            if (form.currentState!.validate()) {
              Navigator.of(context).pop(controller.text.trim());
            }
          },
          child: Text(action),
        ),
      ],
    ),
  ).whenComplete(controller.dispose);
}

/// Copies [text] and confirms with a snack bar.
Future<void> copyText(BuildContext context, String text, {String what = 'Copied'}) async {
  await Clipboard.setData(ClipboardData(text: text));
  if (context.mounted) {
    showMessage(context, '$what.');
  }
}

/// Field label for a VehicleStatus the admin can set.
String vehicleStatusLabel(VehicleStatus status) => switch (status) {
  VehicleStatus.available => 'Available',
  VehicleStatus.active => 'On the road',
  VehicleStatus.inactive => 'Inactive',
  VehicleStatus.underMaintenance => 'Under maintenance',
};

/// FutureBuilder that only starts a new request when [requestKey] changes (filters), not on every rebuild.
/// [retry] restarts the same request.
class KeyedFutureBuilder<T> extends StatefulWidget {
  const KeyedFutureBuilder({super.key, required this.requestKey, required this.load, required this.builder});

  final Object requestKey;
  final Future<T> Function() load;
  final Widget Function(BuildContext context, AsyncSnapshot<T> snapshot, VoidCallback retry) builder;

  @override
  State<KeyedFutureBuilder<T>> createState() => _KeyedFutureBuilderState<T>();
}

class _KeyedFutureBuilderState<T> extends State<KeyedFutureBuilder<T>> {
  late Future<T> _future = widget.load();

  @override
  void didUpdateWidget(KeyedFutureBuilder<T> old) {
    super.didUpdateWidget(old);
    if (old.requestKey != widget.requestKey) {
      _future = widget.load();
    }
  }

  void _retry() => setState(() => _future = widget.load());

  @override
  Widget build(BuildContext context) =>
      FutureBuilder<T>(future: _future, builder: (context, snapshot) => widget.builder(context, snapshot, _retry));
}

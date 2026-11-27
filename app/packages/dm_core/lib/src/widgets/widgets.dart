import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_svg/flutter_svg.dart';

import '../net/api_exception.dart';
import '../repos/repos.dart';
import '../theme/app_theme.dart';
import '../theme/tokens.dart';

/// Surface card: soft shadow in light, 1 px border in dark. [flat] uses surface2 without elevation.
class AppCard extends StatelessWidget {
  const AppCard({super.key, required this.child, this.padding, this.flat = false, this.onTap, this.selected = false});

  final Widget child;
  final EdgeInsetsGeometry? padding;
  final bool flat;
  final bool selected;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    final radius = BorderRadius.circular(DmSize.rCard);
    return DecoratedBox(
      decoration: BoxDecoration(
        color: flat ? c.surface2 : c.surface,
        borderRadius: radius,
        boxShadow: flat ? null : c.cardShadow,
        border: selected
            ? Border.all(color: c.primary, width: 2)
            : (c.isDark && !flat ? Border.all(color: c.border) : null),
      ),
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          borderRadius: radius,
          onTap: onTap,
          child: Padding(padding: padding ?? const EdgeInsets.all(DmSize.cardPadding), child: child),
        ),
      ),
    );
  }
}

/// UAE-style number plate: white plate, dark text, tabular digits.
class PlateBadge extends StatelessWidget {
  const PlateBadge(this.registration, {super.key, this.large = false});

  final String registration;
  final bool large;

  @override
  Widget build(BuildContext context) => Container(
    padding: EdgeInsets.symmetric(horizontal: large ? 12 : 8, vertical: large ? 6 : 3),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(DmSize.rBadge),
      border: Border.all(color: const Color(0xFF111111), width: 1.6),
    ),
    child: Text(
      registration,
      style: dmStyle(large ? 19 : 14, FontWeight.w800, color: const Color(0xFF111111), tabular: true, spacing: .5),
    ),
  );
}

enum Tone { ok, bad, warn, info, neutral, live }

extension ToneColors on Tone {
  (Color background, Color foreground) colors(DmColors c) => switch (this) {
    Tone.ok => (c.pSoft, c.onPSoft),
    Tone.bad => (c.dSoft, c.dText),
    Tone.warn => (c.wSoft, c.warn),
    Tone.info => (c.iSoft, c.info),
    Tone.neutral => (c.neuSoft, c.neuText),
    Tone.live => (c.aSoft, c.aText),
  };
}

/// Status always shows an icon and a word (never colour alone).
class StatusBadge extends StatelessWidget {
  const StatusBadge(this.label, {super.key, required this.tone, this.icon});

  final String label;
  final Tone tone;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final (background, foreground) = tone.colors(context.dm);
    return Container(
      height: 24,
      padding: const EdgeInsets.symmetric(horizontal: 8),
      decoration: BoxDecoration(color: background, borderRadius: BorderRadius.circular(DmSize.rBadge)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[Icon(icon, size: 14, color: foreground), const SizedBox(width: 4)],
          Text(label, style: dmStyle(12, FontWeight.w700, color: foreground)),
        ],
      ),
    );
  }
}

/// Square icon tile used in lists and cards.
class IconTile extends StatelessWidget {
  const IconTile(this.icon, {super.key, this.tone = Tone.ok, this.size = 44});

  final IconData icon;
  final Tone tone;
  final double size;

  @override
  Widget build(BuildContext context) {
    final (background, foreground) = tone.colors(context.dm);
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(color: background, borderRadius: BorderRadius.circular(DmSize.rTile)),
      child: Icon(icon, color: foreground, size: size * .5),
    );
  }
}

/// 8 px capacity bar: primary, warn when nearly full, danger when full.
class CapacityBar extends StatelessWidget {
  const CapacityBar({super.key, required this.fill, this.isFull = false});

  final double fill;
  final bool isFull;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    final color = isFull ? c.danger : (fill >= .85 ? c.warn : c.primary);
    return ClipRRect(
      borderRadius: BorderRadius.circular(4),
      child: LinearProgressIndicator(value: fill.clamp(0, 1), minHeight: 8, color: color, backgroundColor: c.sunken),
    );
  }
}

/// Label on the left, bold value on the right.
class KeyValueRow extends StatelessWidget {
  const KeyValueRow(this.label, this.value, {super.key, this.trailing});

  final String label;
  final String value;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 7),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(child: Text(label, style: context.text.bodySmall)),
        const SizedBox(width: 12),
        Flexible(
          child: trailing ?? Text(value, textAlign: TextAlign.end, style: context.text.titleSmall),
        ),
      ],
    ),
  );
}

/// "Step n of m" bars at the top of a flow.
class StepperBar extends StatelessWidget {
  const StepperBar({super.key, required this.step, required this.total});

  final int step;
  final int total;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    return Row(
      children: [
        for (var i = 1; i <= total; i++) ...[
          Expanded(
            child: Container(
              height: 5,
              decoration: BoxDecoration(
                color: i < step ? c.primary : (i == step ? c.primary.withValues(alpha: .45) : c.sunken),
                borderRadius: BorderRadius.circular(3),
              ),
            ),
          ),
          if (i < total) const SizedBox(width: 6),
        ],
      ],
    );
  }
}

/// Vehicle illustrations from the design (truck, car); the body colour follows the theme's primary.
class Illustration extends StatelessWidget {
  const Illustration(this.name, {super.key, this.height = 110, this.color});

  final String name;
  final double height;
  final Color? color;

  @override
  Widget build(BuildContext context) => SvgPicture.asset(
    'packages/dm_core/assets/illustrations/$name.svg',
    height: height,
    colorMapper: _PrimaryMapper(color ?? context.dm.primary),
  );
}

class _PrimaryMapper extends ColorMapper {
  const _PrimaryMapper(this.primary);

  final Color primary;

  @override
  Color substitute(String? id, String elementName, String attributeName, Color color) =>
      color == const Color(0xFF0B7A62) ? primary : color;
}

/// Image from an API file URL (photos, signatures). Files need the session cookie, so it is attached here.
class AuthImage extends ConsumerWidget {
  const AuthImage(this.path, {super.key, this.fit = BoxFit.cover, this.width, this.height, this.placeholder});

  final String path;
  final BoxFit fit;
  final double? width;
  final double? height;
  final Widget? placeholder;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final api = ref.watch(apiClientProvider);
    final uri = api.fileUri(path);
    final fallback = placeholder ?? SizedBox(width: width, height: height);
    return FutureBuilder<String>(
      future: api.cookieHeader(uri),
      builder: (context, snapshot) => !snapshot.hasData
          ? fallback
          : Image.network(
              uri.toString(),
              headers: {'Cookie': snapshot.data!},
              fit: fit,
              width: width,
              height: height,
              errorBuilder: (_, _, _) => fallback,
            ),
    );
  }
}

/// Round avatar: the person's photo, or their initials on a tint.
class Avatar extends StatelessWidget {
  const Avatar({super.key, required this.name, this.photoUrl, this.size = 44});

  final String name;
  final String? photoUrl;
  final double size;

  @override
  Widget build(BuildContext context) {
    final c = context.dm;
    final initials = name.trim().split(RegExp(r'\s+')).where((p) => p.isNotEmpty).take(2).map((p) => p[0]).join();
    final fallback = Container(
      width: size,
      height: size,
      alignment: Alignment.center,
      decoration: BoxDecoration(color: c.pSoft, shape: BoxShape.circle),
      child: Text(initials.toUpperCase(), style: dmStyle(size * .36, FontWeight.w800, color: c.onPSoft)),
    );
    if (photoUrl == null) {
      return fallback;
    }
    return ClipOval(child: AuthImage(photoUrl!, width: size, height: size, placeholder: fallback));
  }
}

/// What went wrong, what to do (Retry), and the server's reference for support.
class ErrorView extends StatelessWidget {
  const ErrorView({super.key, required this.error, this.onRetry, this.compact = false});

  final Object error;
  final VoidCallback? onRetry;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final api = error is ApiException ? error as ApiException : null;
    final title = api?.code == 'DATABASE_NOT_READY' ? 'Server is starting' : (api?.title ?? 'Something went wrong');
    final detail = api?.detail ?? 'Please try again.';
    final content = Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        IconTile(api?.isTransient ?? false ? Icons.wifi_off_rounded : Icons.error_rounded, tone: Tone.bad, size: 52),
        const SizedBox(height: 14),
        Text(title, style: context.text.titleMedium, textAlign: TextAlign.center),
        const SizedBox(height: 6),
        Text(detail, style: context.text.bodyMedium, textAlign: TextAlign.center),
        if (api?.traceId != null) ...[
          const SizedBox(height: 6),
          SelectableText('Ref: ${api!.traceId}', style: context.text.labelSmall, textAlign: TextAlign.center),
        ],
        if (onRetry != null) ...[
          const SizedBox(height: 16),
          SizedBox(
            width: 180,
            child: FilledButton.icon(onPressed: onRetry, icon: const Icon(Icons.refresh_rounded), label: const Text('Retry')),
          ),
        ],
      ],
    );
    return compact ? content : Center(child: Padding(padding: const EdgeInsets.all(28), child: content));
  }
}

/// Empty state: headline, one line of why, and at most one action.
class EmptyView extends StatelessWidget {
  const EmptyView({super.key, required this.icon, required this.title, required this.message, this.action});

  final IconData icon;
  final String title;
  final String message;
  final Widget? action;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.all(28),
    child: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        IconTile(icon, tone: Tone.neutral, size: 52),
        const SizedBox(height: 14),
        Text(title, style: context.text.titleMedium, textAlign: TextAlign.center),
        const SizedBox(height: 6),
        Text(message, style: context.text.bodyMedium, textAlign: TextAlign.center),
        if (action != null) ...[const SizedBox(height: 16), action!],
      ],
    ),
  );
}

/// Grey placeholder blocks shown while loading (no full-screen spinners).
class SkeletonList extends StatelessWidget {
  const SkeletonList({super.key, this.count = 4, this.height = 84});

  final int count;
  final double height;

  @override
  Widget build(BuildContext context) => ListView.separated(
    padding: const EdgeInsets.all(DmSize.gutter),
    physics: const NeverScrollableScrollPhysics(),
    shrinkWrap: true,
    itemCount: count,
    separatorBuilder: (_, _) => const SizedBox(height: 12),
    itemBuilder: (_, _) => Container(
      height: height,
      decoration: BoxDecoration(color: context.dm.sunken, borderRadius: BorderRadius.circular(DmSize.rCard)),
    ),
  );
}

/// Shows loading / error (with Retry) / data for an [AsyncValue].
class AsyncBody<T> extends StatelessWidget {
  const AsyncBody({super.key, required this.value, required this.data, this.onRetry, this.loading});

  final AsyncValue<T> value;
  final Widget Function(T data) data;
  final VoidCallback? onRetry;
  final Widget? loading;

  @override
  Widget build(BuildContext context) => value.when(
    skipLoadingOnRefresh: true,
    skipLoadingOnReload: true,
    data: data,
    loading: () => loading ?? const SkeletonList(),
    error: (error, _) => ErrorView(error: error, onRetry: onRetry),
  );
}

/// Rebuilds every second, for elapsed-time displays (only timers tick on the device, D-035).
class Ticker extends StatefulWidget {
  const Ticker({super.key, required this.builder});

  final Widget Function(BuildContext context, DateTime nowUtc) builder;

  @override
  State<Ticker> createState() => _TickerState();
}

class _TickerState extends State<Ticker> {
  late final Timer _timer;

  @override
  void initState() {
    super.initState();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) => setState(() {}));
  }

  @override
  void dispose() {
    _timer.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.builder(context, DateTime.now().toUtc());
}

/// Snack bar with the API's message (and reference) for a failed action.
void showError(BuildContext context, Object error) {
  final api = error is ApiException ? error : null;
  final message = api == null ? 'Something went wrong. Please try again.' : api.detail;
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(api?.traceId == null ? message : '$message\nRef: ${api!.traceId}')));
}

void showMessage(BuildContext context, String message) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(message)));
}

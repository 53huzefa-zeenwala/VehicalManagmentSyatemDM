import 'dart:io';
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:dm_core/dm_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gal/gal.dart';
import 'package:go_router/go_router.dart';
import 'package:path_provider/path_provider.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:printing/printing.dart';
import 'package:qr/qr.dart';
import 'package:share_plus/share_plus.dart';

import '../state.dart';
import '../widgets/admin_widgets.dart';
import 'vehicles.dart';

// QR labels are drawn in the app from the API's label data (D-040). Outputs (D-057): Print (Android print dialog),
// Share as image, Save image to the gallery. Size: 50 × 70 mm stickers, 12 per A4 sheet (3 × 4).

const double _labelWmm = 50;
const double _labelHmm = 70;
const int _perRow = 3;
const int _perColumn = 4;
const int labelsPerSheet = _perRow * _perColumn;
const double _a4Wmm = 210;
const double _a4Hmm = 297;

/// Draws one label in [rect]: brand line, QR, registration, category · type, and a thin cut border.
void paintLabel(Canvas canvas, Rect rect, QrLabel label) {
  final unit = rect.width / _labelWmm; // pixels per mm
  canvas
    ..drawRect(rect, Paint()..color = Colors.white)
    ..drawRect(
      rect.deflate(unit * .3),
      Paint()
        ..color = const Color(0xFFBFC9C6)
        ..style = PaintingStyle.stroke
        ..strokeWidth = unit * .25,
    );

  void text(String value, double top, double sizeMm, FontWeight weight, {Color color = const Color(0xFF0D1B1E)}) {
    final painter = TextPainter(
      text: TextSpan(text: value, style: dmStyle(sizeMm * unit, weight, color: color, tabular: true)),
      textAlign: TextAlign.center,
      textDirection: TextDirection.ltr,
      maxLines: 1,
      ellipsis: '…',
    )..layout(maxWidth: rect.width - 6 * unit);
    painter.paint(canvas, Offset(rect.left + (rect.width - painter.width) / 2, rect.top + top * unit));
  }

  text('DM FLEET & PARKING', 4, 2.6, FontWeight.w800, color: const Color(0xFF0B7A62));

  final qr = QrImage(QrCode.fromData(data: label.qrToken, errorCorrectLevel: QrErrorCorrectLevel.M));
  final qrSize = 40 * unit;
  final module = qrSize / qr.moduleCount;
  final origin = Offset(rect.left + (rect.width - qrSize) / 2, rect.top + 9 * unit);
  final dark = Paint()..color = Colors.black;
  for (var y = 0; y < qr.moduleCount; y++) {
    for (var x = 0; x < qr.moduleCount; x++) {
      if (qr.isDark(y, x)) {
        // A hair wider than the module so neighbouring squares never show a seam.
        canvas.drawRect(Rect.fromLTWH(origin.dx + x * module, origin.dy + y * module, module + .5, module + .5), dark);
      }
    }
  }

  text(label.registrationNumber, 52, 6, FontWeight.w800);
  text('${label.category} · ${label.type}', 61, 3, FontWeight.w600, color: const Color(0xFF4B5B5F));
}

Future<Uint8List> _toPng(int width, int height, void Function(Canvas canvas) paint) async {
  final recorder = ui.PictureRecorder();
  paint(Canvas(recorder));
  final picture = recorder.endRecording();
  final image = await picture.toImage(width, height);
  final data = await image.toByteData(format: ui.ImageByteFormat.png);
  image.dispose();
  picture.dispose();
  return data!.buffer.asUint8List();
}

/// One label as a PNG at 300 dpi (591 × 827 px).
Future<Uint8List> renderLabelPng(QrLabel label) {
  const dpmm = 300 / 25.4;
  final w = (_labelWmm * dpmm).round();
  final h = (_labelHmm * dpmm).round();
  return _toPng(w, h, (canvas) => paintLabel(canvas, Rect.fromLTWH(0, 0, w.toDouble(), h.toDouble()), label));
}

/// One A4 sheet (up to 12 labels, 3 × 4) as a PNG at 200 dpi.
Future<Uint8List> renderSheetPng(List<QrLabel> labels) {
  const dpmm = 200 / 25.4;
  final w = (_a4Wmm * dpmm).round();
  final h = (_a4Hmm * dpmm).round();
  const gapMm = 10.0;
  const marginX = (_a4Wmm - _perRow * _labelWmm - (_perRow - 1) * gapMm) / 2;
  const marginY = (_a4Hmm - _perColumn * _labelHmm) / 2;
  return _toPng(w, h, (canvas) {
    canvas.drawRect(Rect.fromLTWH(0, 0, w.toDouble(), h.toDouble()), Paint()..color = Colors.white);
    for (final (i, label) in labels.indexed) {
      final col = i % _perRow;
      final row = i ~/ _perRow;
      final left = (marginX + col * (_labelWmm + gapMm)) * dpmm;
      final top = (marginY + row * _labelHmm) * dpmm;
      paintLabel(canvas, Rect.fromLTWH(left, top, _labelWmm * dpmm, _labelHmm * dpmm), label);
    }
  });
}

List<List<QrLabel>> _sheets(List<QrLabel> labels) => [
  for (var i = 0; i < labels.length; i += labelsPerSheet)
    labels.sublist(i, (i + labelsPerSheet).clamp(0, labels.length)),
];

String _fileSafe(String value) => value.replaceAll(RegExp('[^A-Za-z0-9-]+'), '_');

/// A-12 (one vehicle) and A-13 (many): preview, then Print, Share image, Save image.
class QrLabelScreen extends ConsumerStatefulWidget {
  const QrLabelScreen({super.key, required this.vehicleIds});

  final List<int> vehicleIds;

  @override
  ConsumerState<QrLabelScreen> createState() => _QrLabelScreenState();
}

class _QrLabelScreenState extends ConsumerState<QrLabelScreen> {
  late Future<List<QrLabel>> _labels = _load();

  Future<List<QrLabel>> _load() => ref.read(adminReposProvider).qrLabels(widget.vehicleIds);

  bool get _single => widget.vehicleIds.length == 1;

  /// One PNG per label for a single vehicle; one PNG per A4 sheet otherwise.
  Future<List<(String, Uint8List)>> _images(List<QrLabel> labels) async => _single
      ? [('DM-label-${_fileSafe(labels.first.registrationNumber)}', await renderLabelPng(labels.first))]
      : [
          for (final (i, sheet) in _sheets(labels).indexed)
            ('DM-labels-sheet-${i + 1}', await renderSheetPng(sheet)),
        ];

  Future<void> _print(List<QrLabel> labels) async {
    try {
      final pages = [for (final sheet in _sheets(labels)) await renderSheetPng(sheet)];
      await Printing.layoutPdf(
        name: _single ? 'QR label ${labels.first.registrationNumber}' : 'QR labels (${labels.length})',
        format: PdfPageFormat.a4,
        onLayout: (_) async {
          final doc = pw.Document(title: 'DM QR labels');
          for (final page in pages) {
            doc.addPage(
              pw.Page(
                pageFormat: PdfPageFormat.a4,
                margin: pw.EdgeInsets.zero,
                build: (_) => pw.Image(pw.MemoryImage(page), fit: pw.BoxFit.fill),
              ),
            );
          }
          return doc.save();
        },
      );
    } on Object catch (error) {
      if (mounted) {
        showMessage(context, 'Printing failed: $error');
      }
    }
  }

  Future<void> _share(List<QrLabel> labels) async {
    try {
      final dir = await getTemporaryDirectory();
      final files = <XFile>[];
      for (final (name, png) in await _images(labels)) {
        final file = File('${dir.path}/$name.png');
        await file.writeAsBytes(png, flush: true);
        files.add(XFile(file.path, mimeType: 'image/png'));
      }
      await Share.shareXFiles(files, subject: _single ? 'QR label ${labels.first.registrationNumber}' : 'QR labels');
    } on Object catch (error) {
      if (mounted) {
        showMessage(context, 'Sharing failed: $error');
      }
    }
  }

  Future<void> _save(List<QrLabel> labels) async {
    try {
      if (!await Gal.hasAccess() && !await Gal.requestAccess()) {
        if (mounted) {
          showMessage(context, 'Allow access to photos to save labels.');
        }
        return;
      }
      final images = await _images(labels);
      for (final (name, png) in images) {
        await Gal.putImageBytes(png, name: name);
      }
      if (mounted) {
        showMessage(context, images.length == 1 ? 'Saved to your photos.' : '${images.length} sheets saved to your photos.');
      }
    } on GalException catch (error) {
      if (mounted) {
        showMessage(context, 'Saving failed: ${error.type.message}');
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: Text(_single ? 'QR label' : 'QR labels (${widget.vehicleIds.length})')),
    body: FutureBuilder<List<QrLabel>>(
      future: _labels,
      builder: (context, snapshot) {
        if (snapshot.hasError) {
          return ErrorView(error: snapshot.error!, onRetry: () => setState(() => _labels = _load()));
        }
        if (!snapshot.hasData) {
          return const SkeletonList(count: 1, height: 360);
        }
        final labels = snapshot.data!;
        final sheets = _sheets(labels);
        return Column(
          children: [
            Expanded(
              child: ListView(
                padding: const EdgeInsets.all(DmSize.gutter),
                children: [
                  if (_single)
                    Center(
                      child: Container(
                        decoration: BoxDecoration(boxShadow: context.dm.cardShadow),
                        child: SizedBox(
                          width: 220,
                          child: AspectRatio(
                            aspectRatio: _labelWmm / _labelHmm,
                            child: CustomPaint(painter: _LabelPainter(labels.first)),
                          ),
                        ),
                      ),
                    )
                  else
                    for (final (i, sheet) in sheets.indexed) ...[
                      Text('Sheet ${i + 1} of ${sheets.length} · ${sheet.length} labels', style: context.text.labelMedium),
                      const SizedBox(height: 6),
                      DecoratedBox(
                        decoration: BoxDecoration(boxShadow: context.dm.cardShadow),
                        child: AspectRatio(
                          aspectRatio: _a4Wmm / _a4Hmm,
                          child: CustomPaint(painter: _SheetPainter(sheet)),
                        ),
                      ),
                      const SizedBox(height: 16),
                    ],
                  const SizedBox(height: 14),
                  Text(
                    _single
                        ? 'Label 50 × 70 mm. Print prints it on an A4 sheet; Share and Save give a 300 dpi image.'
                        : 'Labels 50 × 70 mm, 12 per A4 sheet. Share and Save give one image per sheet.',
                    style: context.text.bodySmall,
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            ),
            SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    BusyButton(label: 'Print', icon: Icons.print_rounded, onPressed: () => _print(labels)),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        Expanded(
                          child: BusyButton(
                            label: 'Share image',
                            icon: Icons.share_rounded,
                            outlined: true,
                            onPressed: () => _share(labels),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: BusyButton(
                            label: 'Save image',
                            icon: Icons.download_rounded,
                            outlined: true,
                            onPressed: () => _save(labels),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ],
        );
      },
    ),
  );
}

class _LabelPainter extends CustomPainter {
  const _LabelPainter(this.label);

  final QrLabel label;

  @override
  void paint(Canvas canvas, Size size) => paintLabel(canvas, Offset.zero & size, label);

  @override
  bool shouldRepaint(_LabelPainter old) => old.label.qrToken != label.qrToken;
}

class _SheetPainter extends CustomPainter {
  const _SheetPainter(this.labels);

  final List<QrLabel> labels;

  @override
  void paint(Canvas canvas, Size size) {
    final dpmm = size.width / _a4Wmm;
    const gapMm = 10.0;
    const marginX = (_a4Wmm - _perRow * _labelWmm - (_perRow - 1) * gapMm) / 2;
    const marginY = (_a4Hmm - _perColumn * _labelHmm) / 2;
    canvas.drawRect(Offset.zero & size, Paint()..color = Colors.white);
    for (final (i, label) in labels.indexed) {
      final left = (marginX + (i % _perRow) * (_labelWmm + gapMm)) * dpmm;
      final top = (marginY + (i ~/ _perRow) * _labelHmm) * dpmm;
      paintLabel(canvas, Rect.fromLTWH(left, top, _labelWmm * dpmm, _labelHmm * dpmm), label);
    }
  }

  @override
  bool shouldRepaint(_SheetPainter old) => old.labels != labels;
}

/// A-13: choose vehicles (filters, select all), then make their labels.
class BulkQrScreen extends ConsumerStatefulWidget {
  const BulkQrScreen({super.key});

  @override
  ConsumerState<BulkQrScreen> createState() => _BulkQrScreenState();
}

class _BulkQrScreenState extends ConsumerState<BulkQrScreen> {
  static const int _max = 500; // API limit for qr-labels

  String _search = '';
  int? _typeId;
  int? _lotId;
  final Set<int> _selected = {};
  late Future<List<VehicleDetail>> _vehicles = _load();

  /// Every vehicle matching the filters (up to 500), active and inactive.
  Future<List<VehicleDetail>> _load() async {
    final repos = ref.read(adminReposProvider);
    final all = <VehicleDetail>[];
    for (var page = 1; ; page++) {
      final result = await repos.vehicles(search: _search, typeId: _typeId, lotId: _lotId, page: page, pageSize: 100);
      all.addAll(result.items);
      if (!result.hasMore || all.length >= _max) {
        return all.take(_max).toList();
      }
    }
  }

  void _refilter(VoidCallback change) => setState(() {
    change();
    _vehicles = _load();
  });

  @override
  Widget build(BuildContext context) {
    final types = ref.watch(activeTypeOptionsProvider).valueOrNull ?? const [];
    final lots = ref.watch(allLotsProvider).valueOrNull ?? const [];
    return Scaffold(
      appBar: AppBar(title: const Text('QR labels')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(DmSize.gutter, 4, DmSize.gutter, 8),
            child: Column(
              children: [
                SearchField(hint: 'Registration, brand or model', onChanged: (v) => _refilter(() => _search = v)),
                const SizedBox(height: 10),
                Row(
                  children: [
                    Expanded(
                      child: DropdownButtonFormField<int?>(
                        initialValue: _typeId,
                        isExpanded: true,
                        decoration: const InputDecoration(labelText: 'Type'),
                        items: [
                          const DropdownMenuItem(value: null, child: Text('All types')),
                          for (final t in types) DropdownMenuItem(value: t.id, child: Text(t.label)),
                        ],
                        onChanged: (v) => _refilter(() => _typeId = v),
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: DropdownButtonFormField<int?>(
                        initialValue: _lotId,
                        isExpanded: true,
                        decoration: const InputDecoration(labelText: 'Lot'),
                        items: [
                          const DropdownMenuItem(value: null, child: Text('All lots')),
                          for (final l in lots) DropdownMenuItem(value: l.id, child: Text(l.name)),
                        ],
                        onChanged: (v) => _refilter(() => _lotId = v),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          Expanded(
            child: FutureBuilder<List<VehicleDetail>>(
              future: _vehicles,
              builder: (context, snapshot) {
                if (snapshot.hasError) {
                  return ErrorView(error: snapshot.error!, onRetry: () => _refilter(() {}));
                }
                if (!snapshot.hasData) {
                  return const SkeletonList();
                }
                final vehicles = snapshot.data!;
                if (vehicles.isEmpty) {
                  return const EmptyView(icon: Icons.qr_code_2_rounded, title: 'No vehicles', message: 'Change the filters or add vehicles.');
                }
                final allSelected = vehicles.every((v) => _selected.contains(v.id));
                return Column(
                  children: [
                    CheckboxListTile(
                      value: allSelected,
                      title: Text('Select all ${vehicles.length} shown'),
                      controlAffinity: ListTileControlAffinity.leading,
                      onChanged: (value) => setState(() {
                        if (value ?? false) {
                          _selected.addAll(vehicles.map((v) => v.id).take(_max - _selected.length));
                        } else {
                          _selected.removeAll(vehicles.map((v) => v.id));
                        }
                      }),
                    ),
                    Expanded(
                      child: ListView.separated(
                        padding: const EdgeInsets.fromLTRB(DmSize.gutter, 0, DmSize.gutter, 24),
                        itemCount: vehicles.length,
                        separatorBuilder: (_, _) => const SizedBox(height: 8),
                        itemBuilder: (context, i) {
                          final v = vehicles[i];
                          final selected = _selected.contains(v.id);
                          return VehicleRow(
                            vehicle: v,
                            onTap: () => setState(() => selected ? _selected.remove(v.id) : _selected.add(v.id)),
                            trailing: Checkbox(
                              value: selected,
                              onChanged: (_) => setState(() => selected ? _selected.remove(v.id) : _selected.add(v.id)),
                            ),
                          );
                        },
                      ),
                    ),
                  ],
                );
              },
            ),
          ),
          SafeArea(
            top: false,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(DmSize.gutter, 8, DmSize.gutter, 12),
              child: SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: _selected.isEmpty
                      ? null
                      : () => Navigator.of(context).push(
                          MaterialPageRoute<void>(builder: (_) => QrLabelScreen(vehicleIds: _selected.toList())),
                        ),
                  icon: const Icon(Icons.qr_code_2_rounded),
                  label: Text(_selected.isEmpty ? 'Select vehicles' : 'Make ${_selected.length} labels'),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Opens the single-label screen for a vehicle (used from lists).
void openVehicleLabel(BuildContext context, int vehicleId) => context.push('/vehicles/$vehicleId/label');

import 'package:flutter/material.dart';

/// Emerald Ops colour tokens (D-017), copied from prototype/ui-mock/styles.css. Read with `context.dm`.
@immutable
class DmColors extends ThemeExtension<DmColors> {
  const DmColors({
    required this.bg,
    required this.surface,
    required this.surface2,
    required this.sunken,
    required this.border,
    required this.text,
    required this.text2,
    required this.text3,
    required this.primary,
    required this.onPrimary,
    required this.pDeep,
    required this.pEnd,
    required this.pSoft,
    required this.onPSoft,
    required this.accent,
    required this.onAccent,
    required this.aSoft,
    required this.aText,
    required this.danger,
    required this.dText,
    required this.dSoft,
    required this.warn,
    required this.wSoft,
    required this.info,
    required this.iSoft,
    required this.neuText,
    required this.neuSoft,
    required this.chrome,
    required this.isDark,
  });

  static const light = DmColors(
    bg: Color(0xFFF3F6F5),
    surface: Color(0xFFFFFFFF),
    surface2: Color(0xFFEEF3F1),
    sunken: Color(0xFFE8EFEC),
    border: Color(0xFFE1E8E5),
    text: Color(0xFF0D1B1E),
    text2: Color(0xFF4B5B5F),
    text3: Color(0xFF5F6F73),
    primary: Color(0xFF0B7A62),
    onPrimary: Color(0xFFFFFFFF),
    pDeep: Color(0xFF06503F),
    pEnd: Color(0xFF0C7F64),
    pSoft: Color(0xFFDFF3EC),
    onPSoft: Color(0xFF075A48),
    accent: Color(0xFFFFB020),
    onAccent: Color(0xFF2B1B00),
    aSoft: Color(0xFFFFF1D6),
    aText: Color(0xFF8A4B00),
    danger: Color(0xFFD92D20),
    dText: Color(0xFFB42318),
    dSoft: Color(0xFFFDE7E5),
    warn: Color(0xFFB54708),
    wSoft: Color(0xFFFEF0C7),
    info: Color(0xFF175CD3),
    iSoft: Color(0xFFDCEBFF),
    neuText: Color(0xFF4B5B5F),
    neuSoft: Color(0xFFE8EFEC),
    chrome: Color(0xFFFFFFFF),
    isDark: false,
  );

  static const dark = DmColors(
    bg: Color(0xFF0A1214),
    surface: Color(0xFF111C1F),
    surface2: Color(0xFF172529),
    sunken: Color(0xFF0E181B),
    border: Color(0xFF223337),
    text: Color(0xFFECF3F1),
    text2: Color(0xFFA7B8B5),
    text3: Color(0xFF82938F),
    primary: Color(0xFF34D3A6),
    onPrimary: Color(0xFF022A20),
    pDeep: Color(0xFF0C3B31),
    pEnd: Color(0xFF11684F),
    pSoft: Color(0xFF12352D),
    onPSoft: Color(0xFF7BE8C6),
    accent: Color(0xFFFFB020),
    onAccent: Color(0xFF2B1B00),
    aSoft: Color(0xFF3A2A0D),
    aText: Color(0xFFFFC65C),
    danger: Color(0xFFF04438),
    dText: Color(0xFFFF8A80),
    dSoft: Color(0xFF3D1715),
    warn: Color(0xFFFF9A4D),
    wSoft: Color(0xFF3B210F),
    info: Color(0xFF84ADFF),
    iSoft: Color(0xFF142A4D),
    neuText: Color(0xFFA7B8B5),
    neuSoft: Color(0xFF1C2B2F),
    chrome: Color(0xFF111C1F),
    isDark: true,
  );

  final Color bg;
  final Color surface;
  final Color surface2;
  final Color sunken;
  final Color border;
  final Color text;
  final Color text2;
  final Color text3;
  final Color primary;
  final Color onPrimary;
  final Color pDeep;
  final Color pEnd;
  final Color pSoft;
  final Color onPSoft;

  /// Amber means "live" only (active trip bar, live badges); never text on light surfaces.
  final Color accent;
  final Color onAccent;
  final Color aSoft;
  final Color aText;
  final Color danger;
  final Color dText;
  final Color dSoft;
  final Color warn;
  final Color wSoft;
  final Color info;
  final Color iSoft;
  final Color neuText;
  final Color neuSoft;
  final Color chrome;
  final bool isDark;

  /// Hero gradient (135°): pDeep → primary → pEnd in light, pDeep → pEnd in dark.
  LinearGradient get hero => LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: isDark ? [pDeep, pEnd] : [pDeep, primary, pEnd],
    stops: isDark ? null : const [0, .62, 1],
  );

  /// Card elevation: a soft shadow in light, a 1 px border in dark.
  List<BoxShadow> get cardShadow => isDark
      ? const []
      : const [
          BoxShadow(color: Color(0x0D0D1B1E), blurRadius: 2, offset: Offset(0, 1)),
          BoxShadow(color: Color(0x2E0D1B1E), blurRadius: 28, spreadRadius: -14, offset: Offset(0, 10)),
        ];

  List<BoxShadow> get primaryGlow => [
    BoxShadow(
      color: isDark ? const Color(0x7334D3A6) : const Color(0x8C0B7A62),
      blurRadius: 24,
      spreadRadius: isDark ? -12 : -10,
      offset: const Offset(0, 10),
    ),
  ];

  @override
  DmColors copyWith() => this;

  @override
  DmColors lerp(ThemeExtension<DmColors>? other, double t) => t < .5 ? this : (other as DmColors? ?? this);
}

/// Spacing and radius tokens (4-pt grid).
abstract final class DmSize {
  static const double gutter = 20;
  static const double gap = 14;
  static const double cardPadding = 16;
  static const double rCard = 22;
  static const double rHero = 26;
  static const double rSheet = 30;
  static const double rButton = 17;
  static const double rInput = 16;
  static const double rChip = 12;
  static const double rBadge = 8;
  static const double rTile = 14;
  static const double buttonHeight = 54;
  static const double inputHeight = 52;
}

extension DmThemeX on BuildContext {
  DmColors get dm => Theme.of(this).extension<DmColors>()!;
  TextTheme get text => Theme.of(this).textTheme;
}

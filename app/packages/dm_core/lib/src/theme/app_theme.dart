
import 'package:flutter/material.dart';

import 'tokens.dart';

/// Plus Jakarta Sans is a variable font: weights are set through the `wght` axis as well as [FontWeight].
const String dmFontFamily = 'packages/dm_core/PlusJakartaSans';

TextStyle dmStyle(double size, FontWeight weight, {double? spacing, double? height, Color? color, bool tabular = false}) =>
    TextStyle(
      fontFamily: dmFontFamily,
      fontSize: size,
      fontWeight: weight,
      fontVariations: [FontVariation('wght', weight.value.toDouble())],
      fontFeatures: tabular ? const [FontFeature.tabularFigures()] : null,
      letterSpacing: spacing,
      height: height,
      color: color,
    );

/// Material 3 themes built from the Emerald Ops tokens (D-017), light and dark as complete systems.
abstract final class AppTheme {
  static ThemeData light() => _build(DmColors.light);

  static ThemeData dark() => _build(DmColors.dark);

  static ThemeData _build(DmColors c) {
    final scheme = ColorScheme(
      brightness: c.isDark ? Brightness.dark : Brightness.light,
      primary: c.primary,
      onPrimary: c.onPrimary,
      primaryContainer: c.pSoft,
      onPrimaryContainer: c.onPSoft,
      // Amber means "live" only (D-017), so Material's secondary roles stay emerald.
      secondary: c.primary,
      onSecondary: c.onPrimary,
      secondaryContainer: c.pSoft,
      onSecondaryContainer: c.onPSoft,
      tertiary: c.info,
      onTertiary: Colors.white,
      error: c.danger,
      onError: Colors.white,
      errorContainer: c.dSoft,
      onErrorContainer: c.dText,
      surface: c.surface,
      onSurface: c.text,
      onSurfaceVariant: c.text2,
      surfaceContainerLowest: c.bg,
      surfaceContainerLow: c.surface,
      surfaceContainer: c.surface2,
      surfaceContainerHigh: c.surface2,
      surfaceContainerHighest: c.sunken,
      outline: c.border,
      outlineVariant: c.border,
    );

    final text = TextTheme(
      displaySmall: dmStyle(30, FontWeight.w800, spacing: -.8, color: c.text),
      headlineMedium: dmStyle(28, FontWeight.w800, spacing: -.6, color: c.text),
      headlineSmall: dmStyle(24, FontWeight.w800, color: c.text),
      titleLarge: dmStyle(20, FontWeight.w800, spacing: -.3, color: c.text),
      titleMedium: dmStyle(16.5, FontWeight.w700, color: c.text),
      titleSmall: dmStyle(15, FontWeight.w700, color: c.text),
      bodyLarge: dmStyle(15, FontWeight.w500, color: c.text),
      bodyMedium: dmStyle(14.5, FontWeight.w400, height: 1.45, color: c.text2),
      bodySmall: dmStyle(13, FontWeight.w400, color: c.text2),
      labelLarge: dmStyle(15.5, FontWeight.w700),
      labelMedium: dmStyle(13, FontWeight.w700, color: c.text),
      labelSmall: dmStyle(12, FontWeight.w500, color: c.text3),
    );

    final buttonShape = RoundedRectangleBorder(borderRadius: BorderRadius.circular(DmSize.rButton));
    final inputBorder = OutlineInputBorder(
      borderRadius: BorderRadius.circular(DmSize.rInput),
      borderSide: BorderSide(color: c.border, width: 1.5),
    );

    return ThemeData(
      useMaterial3: true,
      brightness: scheme.brightness,
      colorScheme: scheme,
      scaffoldBackgroundColor: c.bg,
      fontFamily: dmFontFamily,
      textTheme: text,
      extensions: [c],
      appBarTheme: AppBarTheme(
        backgroundColor: c.bg,
        foregroundColor: c.text,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: dmStyle(18, FontWeight.w700, spacing: -.2, color: c.text),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: c.primary,
          foregroundColor: c.onPrimary,
          disabledBackgroundColor: c.sunken,
          disabledForegroundColor: c.text3,
          minimumSize: const Size.fromHeight(DmSize.buttonHeight),
          shape: buttonShape,
          textStyle: text.labelLarge,
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          backgroundColor: c.isDark ? c.surface2 : c.surface,
          foregroundColor: c.text,
          side: BorderSide(color: c.border, width: 1.5),
          minimumSize: const Size.fromHeight(DmSize.buttonHeight),
          shape: buttonShape,
          textStyle: text.labelLarge,
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(foregroundColor: c.primary, textStyle: dmStyle(14, FontWeight.w700)),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: c.isDark ? c.surface2 : c.surface,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 15),
        border: inputBorder,
        enabledBorder: inputBorder,
        focusedBorder: inputBorder.copyWith(borderSide: BorderSide(color: c.primary, width: 2)),
        errorBorder: inputBorder.copyWith(borderSide: BorderSide(color: c.danger, width: 1.5)),
        focusedErrorBorder: inputBorder.copyWith(borderSide: BorderSide(color: c.danger, width: 2)),
        labelStyle: dmStyle(14, FontWeight.w500, color: c.text2),
        hintStyle: dmStyle(15, FontWeight.w400, color: c.text3),
        errorStyle: dmStyle(12.5, FontWeight.w600, color: c.dText),
        prefixIconColor: c.text3,
        suffixIconColor: c.text3,
      ),
      chipTheme: ChipThemeData(
        backgroundColor: c.isDark ? c.surface2 : c.surface,
        selectedColor: c.text,
        labelStyle: dmStyle(13.5, FontWeight.w600, color: c.text),
        secondaryLabelStyle: dmStyle(13.5, FontWeight.w600, color: c.bg),
        side: BorderSide(color: c.border),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(DmSize.rChip)),
        showCheckmark: false,
      ),
      cardTheme: CardThemeData(
        color: c.surface,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(DmSize.rCard),
          side: c.isDark ? BorderSide(color: c.border) : BorderSide.none,
        ),
      ),
      bottomSheetTheme: BottomSheetThemeData(
        backgroundColor: c.isDark ? c.surface2 : c.surface,
        showDragHandle: true,
        dragHandleColor: c.border,
        dragHandleSize: const Size(44, 5),
        shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(DmSize.rSheet))),
      ),
      segmentedButtonTheme: SegmentedButtonThemeData(
        style: ButtonStyle(
          minimumSize: const WidgetStatePropertyAll(Size(0, 44)),
          textStyle: WidgetStatePropertyAll(dmStyle(14, FontWeight.w700)),
          side: WidgetStatePropertyAll(BorderSide(color: c.border)),
          shape: WidgetStatePropertyAll(RoundedRectangleBorder(borderRadius: BorderRadius.circular(14))),
          backgroundColor: WidgetStateProperty.resolveWith(
            (s) => s.contains(WidgetState.selected) ? c.text : (c.isDark ? c.surface2 : c.surface),
          ),
          foregroundColor: WidgetStateProperty.resolveWith((s) => s.contains(WidgetState.selected) ? c.bg : c.text),
        ),
      ),
      dividerTheme: DividerThemeData(color: c.border, thickness: 1, space: 1),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: c.isDark ? c.surface2 : c.text,
        contentTextStyle: dmStyle(14, FontWeight.w600, color: c.isDark ? c.text : c.bg),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      ),
      progressIndicatorTheme: ProgressIndicatorThemeData(color: c.primary),
      radioTheme: RadioThemeData(fillColor: WidgetStatePropertyAll(c.primary)),
      checkboxTheme: CheckboxThemeData(
        fillColor: WidgetStateProperty.resolveWith((s) => s.contains(WidgetState.selected) ? c.primary : null),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(5)),
      ),
    );
  }
}

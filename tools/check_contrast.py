#!/usr/bin/env python3
"""Verify WCAG 2.x contrast for the "Emerald Ops" colour pairs listed in docs/design/DESIGN_SYSTEM.md.

Usage: python3 -I tools/check_contrast.py   (exit code 1 if any pair fails)
Keep PAIRS in sync with prototype/ui-mock/styles.css when tokens change.
"""
import sys

# (label, foreground, background, minimum ratio)
PAIRS = [
    ("light text / bg", "#0D1B1E", "#F3F6F5", 4.5),
    ("light text2 / bg", "#4B5B5F", "#F3F6F5", 4.5),
    ("light text3 / surface", "#5F6F73", "#FFFFFF", 4.5),
    ("light white / primary", "#FFFFFF", "#0B7A62", 4.5),
    ("light white / hero deep", "#FFFFFF", "#06503F", 4.5),
    ("light primary / surface", "#0B7A62", "#FFFFFF", 4.5),
    ("light onPrimarySoft / primarySoft", "#075A48", "#DFF3EC", 4.5),
    ("light onAccent / accent", "#2B1B00", "#FFB020", 4.5),
    ("light accentText / accentSoft", "#8A4B00", "#FFF1D6", 4.5),
    ("light danger / dangerSoft", "#B42318", "#FDE7E5", 4.5),
    ("light warning / warningSoft", "#B54708", "#FEF0C7", 4.5),
    ("light info / infoSoft", "#175CD3", "#DCEBFF", 4.5),
    ("light white / danger", "#FFFFFF", "#D92D20", 4.5),
    ("dark text / bg", "#ECF3F1", "#0A1214", 4.5),
    ("dark text2 / surface", "#A7B8B5", "#111C1F", 4.5),
    ("dark text3 / surface", "#82938F", "#111C1F", 4.5),
    ("dark text3 / surface2", "#82938F", "#172529", 4.5),
    ("dark onPrimary / primary", "#022A20", "#34D3A6", 4.5),
    ("dark primary / surface", "#34D3A6", "#111C1F", 4.5),
    ("dark onPrimarySoft / primarySoft", "#7BE8C6", "#12352D", 4.5),
    ("dark accentText / accentSoft", "#FFC65C", "#3A2A0D", 4.5),
    ("dark danger / dangerSoft", "#FF8A80", "#3D1715", 4.5),
    ("dark warning / warningSoft", "#FF9A4D", "#3B210F", 4.5),
    ("dark info / infoSoft", "#84ADFF", "#142A4D", 4.5),
    ("dark white / hero deep", "#FFFFFF", "#0C3B31", 4.5),
    ("dark white / hero end", "#FFFFFF", "#11684F", 4.5),
]


def luminance(hex_colour: str) -> float:
    h = hex_colour.lstrip("#")
    channels = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    lin = [c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4 for c in channels]
    return 0.2126 * lin[0] + 0.7152 * lin[1] + 0.0722 * lin[2]


def ratio(a: str, b: str) -> float:
    hi, lo = sorted((luminance(a), luminance(b)), reverse=True)
    return (hi + 0.05) / (lo + 0.05)


def main() -> int:
    failed = 0
    for label, fg, bg, minimum in PAIRS:
        r = ratio(fg, bg)
        ok = r >= minimum
        failed += not ok
        print(f"{'OK  ' if ok else 'FAIL'} {r:5.2f}:1  (min {minimum})  {label}")
    print(f"{len(PAIRS) - failed}/{len(PAIRS)} pairs pass")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())

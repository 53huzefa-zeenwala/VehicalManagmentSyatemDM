#!/usr/bin/env python3
"""Render the dummy-app screens to PNG (3x) and compile the UI mock-up PDF.

Usage:  python3 -I tools/render_mockups.py [--check-only]
Needs:  Google Chrome (headless), Pillow, an internet connection (Google Fonts).
Output: docs/design/mockups/png/{light,dark,alt}/*.png and docs/design/mockups/UI_Mockups_v1.pdf
"""
import html
import json
import re
import subprocess
import sys
import tempfile
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from urllib.parse import quote

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
MOCK = ROOT / "prototype" / "ui-mock"
OUT = ROOT / "docs" / "design" / "mockups"
PNG = OUT / "png"
JPG = MOCK / "renders"          # working copies used by deck.html
PDF = OUT / "UI_Mockups_v1.pdf"
CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
W, H, SCALE = 390, 844, 3

# (id, title, caption) grouped into deck pages of four.
PAGES = [
    ("Staff · Start a trip", "The staff member is the driver. Scan, choose a destination, sign, start.", [
        ("staff-home", "S-01 Home", "Ready state. One big action: scan to start. Last trip, transfer and lot space below."),
        ("scan", "C-09 Scan", "Full-screen scanner. Typing the registration number is one tap away."),
        ("start-vehicle", "S-03 Vehicle", "Step 1 of 4. Photo-style card and plate badge make the wrong vehicle obvious."),
        ("start-destination", "S-05 Destination", "Step 2 of 4. Area chips, then a location list."),
        ("start-signature", "S-06 Signature", "Step 3 of 4. The staff member signs to confirm they're taking it."),
    ]),
    ("Staff · Trip running", "An amber live bar with the timer is pinned on every staff screen until the trip ends.", [
        ("start-review", "S-07 Review", "Step 4 of 4. Everything in one place, each line editable."),
        ("staff-home-active", "S-01 Trip running", "Big timer, route, and End / Swap / Cancel."),
        ("active-overdue", "S-09 Overdue", "Timer ring, overdue state, and a segment timeline (here, a hand-over)."),
        ("swap", "S-14 Swap", "Change the vehicle, or hand the trip over to another staff member."),
    ]),
    ("Staff · Ending a trip", "Ending takes three taps. Cancelling explains what happens.", [
        ("end-return", "S-11 Return lot", "Origin lot pre-selected. A full lot warns but doesn't block."),
        ("trip-summary", "S-13 Summary", "Every field the client asked for, right after ending."),
        ("cancel-trip", "S-15 Cancel", "Reason, note and where the vehicle is now. Hours still count."),
        ("my-trips", "S-16 My trips", "My trips, with each outcome labelled."),
    ]),
    ("Staff · Everyday", "Lot space, my profile and sign-in.", [
        ("lots", "S-18 Lots", "Map plus free spaces per lot and per vehicle type."),
        ("profile", "S-24 Profile", "Profile tab: my details, licence, assigned lots, my trips, settings."),
        ("login", "C-02 Sign in", "One app for both roles. The account decides the role."),
    ]),
    ("Admin · Monitor", "Admins monitor only: no scanning, no ending trips. What needs attention comes first.", [
        ("admin-dashboard", "A-01 Dashboard", "Live count, activity chart, and the issues that need action."),
        ("admin-live", "A-02 Live", "Every trip out right now, overdue first, timers ticking."),
        ("admin-trip", "A-04 Trip detail", "Read-only: staff, vehicle, route and the reminder log."),
        ("admin-transfer", "A-06 New transfer", "Vehicle and destination lot. Whoever at the origin confirms departure drives it."),
    ]),
    ("Admin · Manage and report", "Master data, vehicle and lot detail, on-screen reports.", [
        ("admin-manage", "A-08 Manage", "Fleet, people, places and system in one hub."),
        ("admin-vehicle", "A-10 Vehicle", "Resolve \"Not at a lot\", set the status, and get the QR label."),
        ("admin-lot", "A-18 Lot", "Free spaces in total and per vehicle type, plus details."),
        ("admin-report", "R-1 Report", "Filters, totals, chart and ranking. On screen only (v1)."),
    ]),
]
ALT = [("ocean", "light", "staff-home-active"), ("ocean", "dark", "admin-dashboard"),
       ("lime", "light", "staff-home-active"), ("lime", "dark", "admin-dashboard")]


def sync_icon_subset():
    """Keep the Material Symbols icon_names subset in screens.html equal to the icons actually used."""
    page = MOCK / "screens.html"
    src = page.read_text()
    used = set(re.findall(r'class="ms[^"]*"[^>]*>([a-z0-9_]+)<', src))
    used |= set(re.findall(r"'([a-z0-9_]+)'", (MOCK / "app.js").read_text())) & {
        "home", "route", "local_parking", "person", "space_dashboard", "sensors", "tune", "bar_chart"}
    used |= {"qr_code_scanner", "signal_cellular_alt", "wifi", "battery_full"}  # injected by app.js
    names = ",".join(sorted(used))
    new = re.sub(r"icon_names=[a-z0-9_,]+", f"icon_names={names}", src)
    if new != src:
        page.write_text(new)
        print(f"icon subset updated ({len(used)} icons)")


def file_url(path: Path, query: str = "") -> str:
    return "file://" + quote(str(path)) + (("?" + query) if query else "")


def chrome(args, profile, wait="--virtual-time-budget=8000", limit=60):
    # Virtual time waits for fonts (keep the icon font subset small via icon_names).
    # It occasionally stalls on a network fetch, so callers fall back to a fixed --timeout.
    cmd = [CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
           "--no-default-browser-check", f"--user-data-dir={profile}", wait, *args]
    return subprocess.run(cmd, capture_output=True, text=True, timeout=limit)


def shot(job):
    query, out = job
    out.parent.mkdir(parents=True, exist_ok=True)
    args = [f"--window-size={W},{H}", f"--force-device-scale-factor={SCALE}",
            f"--screenshot={out}", file_url(MOCK / "screens.html", query)]
    last = ""
    for wait in ("--virtual-time-budget=8000", "--timeout=9000", "--timeout=15000"):
        if out.exists():
            out.unlink()
        with tempfile.TemporaryDirectory() as prof:
            try:
                last = chrome(args, prof, wait).stderr[-300:]
            except subprocess.TimeoutExpired:
                last = f"timeout with {wait}"
        if out.exists():
            return out
    raise RuntimeError(f"render failed for {query}: {last}")


def check_overflow():
    issues = {}
    for theme in ("light", "dark"):
        with tempfile.TemporaryDirectory() as prof:
            r = chrome(["--window-size=1600,1200", "--dump-dom",
                        file_url(MOCK / "screens.html", f"check=1&t={theme}")], prof)
        m = re.search(r'data-check="([^"]*)"', r.stdout)
        issues[theme] = json.loads(html.unescape(m.group(1))) if m else "no result"
    return issues


def to_jpg(png: Path) -> Path:
    rel = png.relative_to(PNG)
    jpg = (JPG / rel).with_suffix(".jpg")
    jpg.parent.mkdir(parents=True, exist_ok=True)
    Image.open(png).convert("RGB").save(jpg, "JPEG", quality=88, optimize=True)
    return jpg


def phone(src: str, title: str, caption: str, dark: bool) -> str:
    return (f'<figure class="ph"><div class="frame"><img src="{src}" alt="{html.escape(title)}"></div>'
            f'<figcaption><b>{html.escape(title)}</b><span>{html.escape(caption)}</span></figcaption></figure>')


def build_deck(index):
    pages = []
    # Cover
    pages.append(f"""<section class="page cover"><div class="ctext">
      <div class="kicker">UI mock-ups · v1 · 22 Nov 2026</div>
      <h1>Vehicle &amp; Parking<br>Management</h1>
      <p>One app for staff and admins. Rendered from the dummy app in <code>prototype/ui-mock</code>.
      Visual direction <b>"Emerald Ops"</b>: deep emerald for trust and action, amber for anything live.</p>
      <div class="legend"><span><i style="background:#0B7A62"></i>Emerald · primary</span><span><i style="background:#FFB020"></i>Amber · live</span><span><i style="background:#0A1214"></i>Night · dark mode</span></div>
      <p class="small">Names, numbers and people are sample data. The app name and logo are placeholders (OI-04).</p></div>
      <div class="trio"><img class="a" src="{index[('light','staff-home-active')]}"><img class="b" src="{index[('dark','admin-dashboard')]}"><img class="c" src="{index[('light','trip-summary')]}"></div></section>""")
    # Design language
    pages.append("""<section class="page lang"><header><h2>Design language</h2><p>Light and dark are designed as complete themes. Every text colour pairing passes WCAG AA (computed).</p></header>
      <div class="grid3">
       <div class="blk"><h3>Colour · light</h3><div class="sw"><i style="background:#06503F">Emerald deep<br>#06503F</i><i style="background:#0B7A62">Emerald<br>#0B7A62</i><i style="background:#DFF3EC;color:#075A48">Emerald soft<br>#DFF3EC</i><i style="background:#FFB020;color:#2B1B00">Amber live<br>#FFB020</i><i style="background:#F3F6F5;color:#0D1B1E">Background<br>#F3F6F5</i><i style="background:#0D1B1E">Ink text<br>#0D1B1E</i></div>
       <h3>Colour · dark</h3><div class="sw"><i style="background:#0A1214">Night<br>#0A1214</i><i style="background:#111C1F">Surface<br>#111C1F</i><i style="background:#34D3A6;color:#022A20">Mint<br>#34D3A6</i><i style="background:#FFB020;color:#2B1B00">Amber<br>#FFB020</i><i style="background:#12352D;color:#7BE8C6">Mint soft<br>#12352D</i><i style="background:#ECF3F1;color:#0D1B1E">Text<br>#ECF3F1</i></div></div>
       <div class="blk"><h3>Status</h3><div class="st"><span style="background:#DFF3EC;color:#075A48">Available · completed</span><span style="background:#FFF1D6;color:#8A4B00">Live · on trip</span><span style="background:#FDE7E5;color:#B42318">Overdue · cancelled</span><span style="background:#FEF0C7;color:#B54708">Full · maintenance</span><span style="background:#DCEBFF;color:#175CD3">Transfer · admin-ended</span><span style="background:#E8EFEC;color:#4B5B5F">Inactive · not at a lot</span></div>
       <h3>Type · Plus Jakarta Sans</h3><div class="ty"><b style="font-size:44px;letter-spacing:-1.5px">01:24:37</b><b style="font-size:28px;letter-spacing:-.6px">Screen title</b><b style="font-size:18px">Section heading</b><span style="font-size:15px">Body text for lists and forms.</span><span style="font-size:12.5px;color:#5F6F73">Caption · meta</span></div></div>
       <div class="blk"><h3>Signature elements</h3><ul>
        <li><b>Live bar</b>: amber pill with plate and timer, pinned while a vehicle is out. It turns red when overdue.</li>
        <li><b>Plate badge</b>: registrations drawn as plates, so vehicles read as vehicles.</li>
        <li><b>Hero card</b>: emerald gradient with route lines for the one thing that matters now.</li>
        <li><b>Scan button</b>: raised centre tab for staff, since scanning is the core action. Admins don't scan.</li>
        <li><b>Status</b>: always icon + word, never colour alone.</li></ul>
        <h3>Shape and touch</h3><ul><li>Cards 22 px radius, buttons 17 px, 54 px tall.</li><li>Touch targets ≥ 44 px. 4-pt spacing.</li><li>Material Symbols Rounded icons.</li></ul></div>
      </div></section>""")
    for theme in ("light", "dark"):
        for title, sub, items in PAGES:
            figs = "".join(phone(index[(theme, sid)], t, c, theme == "dark") for sid, t, c in items)
            label = "Dark mode · " if theme == "dark" else ""
            five = {5: " five", 3: " three"}.get(len(items), "")
            pages.append(f'<section class="page {theme}{five}"><header><h2>{label}{html.escape(title)}</h2><p>{html.escape(sub)}</p></header><div class="row">{figs}</div></section>')
    alt_figs = "".join(phone(index[("alt", f"{p}-{t}-{s}")], f"{p.title()} · {t}", "Same screen, alternative palette.", t == "dark") for p, t, s in ALT)
    pages.append(f'<section class="page light"><header><h2>Alternative palettes</h2><p>If you prefer another colour story: Ocean (blue + orange) or Midnight Lime. Switching is a token change.</p></header><div class="row">{alt_figs}</div></section>')

    css = (MOCK / "deck.css").read_text()
    doc = f"""<!doctype html><html><head><meta charset="utf-8"><title>UI Mock-ups v1</title>
<link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=block" rel="stylesheet">
<style>{css}</style></head><body>{''.join(pages)}</body></html>"""
    deck = MOCK / "deck.html"
    deck.write_text(doc)
    return deck


def main():
    """Flags: --check-only (layout check), --deck-only (reuse existing PNGs, rebuild the PDF)."""
    jobs = []
    ids = [sid for _, _, items in PAGES for sid, _, _ in items]
    for theme in ("light", "dark"):
        for i, sid in enumerate(ids, 1):
            jobs.append((f"s={sid}&t={theme}", PNG / theme / f"{i:02d}-{sid}.png"))
    for p, t, s in ALT:
        jobs.append((f"s={s}&t={t}&p={p}", PNG / "alt" / f"{p}-{t}-{s}.png"))
    if "--deck-only" not in sys.argv:
        sync_icon_subset()
        issues = check_overflow()
        print("overflow check:", json.dumps(issues))
        if "--check-only" in sys.argv:
            return
        # Clear previously generated images so screens removed from PAGES don't linger.
        for folder in (PNG / "light", PNG / "dark", PNG / "alt"):
            for old in folder.glob("*.png"):
                old.unlink()
        for old in JPG.rglob("*.jpg"):
            old.unlink()
        with ThreadPoolExecutor(max_workers=4) as ex:
            done = list(ex.map(shot, jobs))
        print(f"rendered {len(done)} screens")

    index = {}
    for (query, png) in jobs:
        jpg = to_jpg(png).relative_to(MOCK).as_posix()
        params = dict(kv.split("=") for kv in query.split("&"))
        if "p" in params:
            index[("alt", f"{params['p']}-{params['t']}-{params['s']}")] = jpg
        else:
            index[(params["t"], params["s"])] = jpg
    deck = build_deck(index)
    with tempfile.TemporaryDirectory() as prof:
        chrome(["--no-pdf-header-footer", f"--print-to-pdf={PDF}", file_url(deck)], prof)
    print("pdf:", PDF, f"{PDF.stat().st_size / 1e6:.1f} MB" if PDF.exists() else "MISSING")


if __name__ == "__main__":
    main()

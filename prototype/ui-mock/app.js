// Dummy-app helpers: renders shared chrome, QR codes, maps and charts, then
// shows one screen per URL (?s=<id>&t=light|dark&p=emerald|ocean|lime).
(function () {
  var q = new URLSearchParams(location.search);
  var only = q.get('s'), theme = q.get('t') || 'light', palette = q.get('p') || 'emerald';

  document.querySelectorAll('.screen').forEach(function (s) {
    s.setAttribute('data-theme', theme);
    s.setAttribute('data-palette', palette);
  });
  if (only) {
    document.body.classList.add('solo');
    document.body.setAttribute('data-theme', theme);
    var el = document.getElementById(only);
    if (el) el.classList.add('show');
  } else {
    document.body.classList.add('gallery');
  }

  document.querySelectorAll('[data-sbar]').forEach(function (b) {
    b.innerHTML = '<span class="num">9:41</span><span class="island"></span>' +
      '<span class="ic"><span class="ms f">signal_cellular_alt</span><span class="ms f">wifi</span><span class="ms f" style="font-size:24px">battery_full</span></span>';
  });

  // null = raised centre Scan button (staff only; admins don't scan, D-021/D-022).
  var TABS = {
    staff: [['home', 'home', 'Home'], ['trips', 'route', 'Trips'], null, ['lots', 'local_parking', 'Lots'], ['profile', 'person', 'Profile']],
    admin: [['dash', 'space_dashboard', 'Dashboard'], ['live', 'sensors', 'Live'], ['manage', 'tune', 'Manage'], ['reports', 'bar_chart', 'Reports']]
  };
  document.querySelectorAll('[data-tabs]').forEach(function (slot) {
    var parts = slot.getAttribute('data-tabs').split(':');
    var html = TABS[parts[0]].map(function (t) {
      if (!t) return '<div class="fab"><span><span class="ms">qr_code_scanner</span></span></div>';
      return '<div class="tab' + (t[0] === parts[1] ? ' on' : '') + '"><span class="ms">' + t[1] + '</span>' + t[2] + '</div>';
    }).join('');
    slot.outerHTML = '<div class="tabbar">' + html + '</div><div class="homeind"></div>';
  });

  // Deterministic QR-like pattern (illustrative only).
  function rng(seed) { var x = seed % 2147483647; return function () { x = x * 16807 % 2147483647; return x / 2147483647; }; }
  document.querySelectorAll('svg.qr').forEach(function (svg) {
    var r = rng(parseInt(svg.getAttribute('data-seed'), 10) || 7), n = 21, out = '';
    function finder(x, y) {
      out += '<rect x="' + x + '" y="' + y + '" width="7" height="7" fill="#111"/><rect x="' + (x + 1) + '" y="' + (y + 1) + '" width="5" height="5" fill="#fff"/><rect x="' + (x + 2) + '" y="' + (y + 2) + '" width="3" height="3" fill="#111"/>';
    }
    for (var y = 0; y < n; y++) for (var x = 0; x < n; x++) {
      var inF = (x < 8 && y < 8) || (x > 12 && y < 8) || (x < 8 && y > 12);
      if (!inF && r() > 0.52) out += '<rect x="' + x + '" y="' + y + '" width="1.02" height="1.02" fill="#111"/>';
    }
    finder(0, 0); finder(14, 0); finder(0, 14);
    svg.innerHTML = out;
  });

  // Stylised map.
  document.querySelectorAll('svg.map').forEach(function (svg) {
    var one = svg.getAttribute('data-one');
    var vb = svg.viewBox.baseVal, w = vb.width, h = vb.height;
    var s = '<rect width="' + w + '" height="' + h + '" fill="var(--map1)"/>' +
      '<path d="M' + (w * 0.62) + ' 0C' + (w * 0.7) + ' ' + (h * 0.3) + ' ' + (w * 0.58) + ' ' + (h * 0.55) + ' ' + (w * 0.75) + ' ' + h + 'H' + w + 'V0z" fill="var(--water)"/>' +
      '<rect x="' + (w * 0.08) + '" y="' + (h * 0.58) + '" width="' + (w * 0.2) + '" height="' + (h * 0.28) + '" rx="10" fill="var(--park)"/>' +
      '<g stroke="var(--map2)" stroke-linecap="round" fill="none"><path d="M-10 ' + (h * 0.42) + 'L' + (w + 10) + ' ' + (h * 0.3) + '" stroke-width="10"/><path d="M' + (w * 0.35) + ' -10L' + (w * 0.42) + ' ' + (h + 10) + '" stroke-width="10"/>' +
      '<path d="M-10 ' + (h * 0.8) + 'L' + (w * 0.55) + ' ' + (h * 0.7) + '" stroke-width="6"/><path d="M' + (w * 0.15) + ' -10L' + (w * 0.2) + ' ' + (h * 0.5) + '" stroke-width="6"/><path d="M' + (w * 0.5) + ' ' + (h * 0.05) + 'L' + (w * 0.58) + ' ' + (h * 0.55) + '" stroke-width="5"/></g>';
    function pin(x, y, label, color) {
      return '<g transform="translate(' + x + ',' + y + ')"><path d="M0 0c-10-12-16-18-16-27a16 16 0 1 1 32 0c0 9-6 15-16 27z" fill="' + color + '" stroke="#fff" stroke-width="2.5"/>' +
        '<text x="0" y="-22" text-anchor="middle" font-family="Plus Jakarta Sans" font-weight="800" font-size="11" fill="#fff">' + label + '</text></g>';
    }
    if (one) s += pin(w * 0.4, h * 0.62, 'P', 'var(--primary)');
    else s += pin(w * 0.3, h * 0.5, '38', 'var(--primary)') + pin(w * 0.52, h * 0.82, '5', '#D97706') + pin(w * 0.2, h * 0.28, '21', 'var(--primary)');
    svg.innerHTML = s;
  });

  // Bar chart.
  document.querySelectorAll('svg.bars').forEach(function (svg) {
    var data = [142, 158, 171, 150, 166, 188, 120, 176, 181, 169, 190, 197, 132, 184, 201, 178, 193, 205, 141, 199, 212, 186];
    var max = 220, bw = 10, gap = 5, out = '';
    data.forEach(function (v, i) {
      var hgt = Math.round(v / max * 100), x = i * (bw + gap);
      out += '<rect x="' + x + '" y="' + (106 - hgt) + '" width="' + bw + '" height="' + hgt + '" rx="3" fill="' + (i === data.length - 1 ? 'var(--primary)' : 'var(--pSoft)') + '"/>';
    });
    out += '<line x1="0" y1="107" x2="330" y2="107" stroke="var(--border)"/><text x="0" y="119" font-size="9" fill="var(--text3)" font-family="Plus Jakarta Sans">1 Nov</text><text x="330" y="119" text-anchor="end" font-size="9" fill="var(--text3)" font-family="Plus Jakarta Sans">22 Nov</text>';
    svg.innerHTML = out;
  });

  // ?check=1 → record content overflow per screen (read by tools/render_mockups.py via --dump-dom).
  if (q.get('check')) {
    document.fonts.ready.then(function () {
      setTimeout(function () {
        var res = [];
        document.querySelectorAll('.screen').forEach(function (s) {
          var b = s.querySelector('.body');
          if (!b) return;
          var v = b.scrollHeight - b.clientHeight, hz = b.scrollWidth - b.clientWidth;
          if (v > 1 || hz > 1) res.push({ id: s.id, vertical: v, horizontal: hz });
        });
        document.body.setAttribute('data-check', JSON.stringify(res));
      }, 300);
    });
  }
})();

// Reads the browser's CSS env(safe-area-inset-*) values so Unity WebGL can
// honour device notches / rounded corners / home indicators.
//
// Requires <meta name="viewport" content="... viewport-fit=cover"> in the page
// (added via the poki-template extra_html.head). Without it every inset is 0.
//
// Returns the inset for one edge in DEVICE pixels, to match Screen.safeArea units:
//   edge 0 = top, 1 = right, 2 = bottom, 3 = left
mergeInto(LibraryManager.library, {
  SafeAreaBridge_Inset: function (edge) {
    try {
      var probe = document.getElementById('unity-safe-area-probe');
      if (!probe) {
        probe = document.createElement('div');
        probe.id = 'unity-safe-area-probe';
        probe.style.cssText =
          'position:fixed;left:0;top:0;width:0;height:0;visibility:hidden;' +
          'pointer-events:none;' +
          'padding-top:env(safe-area-inset-top,0px);' +
          'padding-right:env(safe-area-inset-right,0px);' +
          'padding-bottom:env(safe-area-inset-bottom,0px);' +
          'padding-left:env(safe-area-inset-left,0px);';
        document.body.appendChild(probe);
      }

      var cs = getComputedStyle(probe);
      var cssPx;
      switch (edge) {
        case 0:  cssPx = parseFloat(cs.paddingTop);    break;
        case 1:  cssPx = parseFloat(cs.paddingRight);  break;
        case 2:  cssPx = parseFloat(cs.paddingBottom); break;
        default: cssPx = parseFloat(cs.paddingLeft);   break;
      }
      if (!isFinite(cssPx) || cssPx < 0) cssPx = 0;

      return cssPx * (window.devicePixelRatio || 1);
    } catch (e) {
      return 0;
    }
  }
});

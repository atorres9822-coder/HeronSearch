/*
 * charts.js: draws the HeronSearch Admin charts with Chart.js 4.
 *
 * Load order: ~/lib/chart.js/dist/chart.umd.min.js (exposes the global `Chart`),
 * then this file, both at the end of <body> via @section Scripts.
 *
 * Markup (rendered by the _Chart partial):
 *   <div class="hs-chart-box">
 *     <canvas class="js-chart" role="img" aria-label="(alt text)" data-chart="(JSON)"></canvas>
 *   </div>
 * .hs-chart-box has a fixed height, so every chart uses responsive: true and
 * maintainAspectRatio: false. The canvas role and aria-label (the chart's text
 * alternative) are never modified here.
 *
 * data-chart JSON (ChartViewModel.ToJson(), System.Text.Json camelCase; Razor
 * HTML-encodes it in the attribute and the browser decodes it for dataset.chart):
 *   {
 *     "kind": "line" | "bar" | "horizontalBar",
 *     "labels": ["May 2026", "Jun 2026", ...],
 *     "series": [ { "name": "Lost", "values": [3, 1, 0, ...] }, ... ]
 *   }
 *
 * Rendering rules:
 *   - kind "line" -> Chart.js "line"; "bar" -> "bar";
 *     "horizontalBar" -> "bar" with indexAxis: 'y'.
 *   - Series i uses palette[i % 4] = Maroon, Gold, Deep Green, Sand, read from the
 *     --hs-maroon, --hs-gold, --hs-green and --hs-sand CSS variables on :root
 *     (hex fallbacks when a variable is empty).
 *   - The value axis (x for horizontalBar, y otherwise) begins at zero with
 *     integer ticks (precision: 0); the category axis keeps Chart.js defaults.
 *   - The legend is displayed only when there are two or more series.
 *   - animation: false when the user prefers reduced motion.
 *   - A canvas with invalid JSON, an unknown kind, or missing labels/series
 *     arrays is skipped with a console warning.
 *   - Running the script again destroys and redraws any existing chart.
 */

// IIFE: keeps every helper out of the global scope.
(function () {
  'use strict';

  // Series palette in order (Maroon, Gold, Deep Green, Sand): CSS variable plus hex fallback.
  const PALETTE = [
    { cssVar: '--hs-maroon', fallback: '#6B1A1A' },
    { cssVar: '--hs-gold', fallback: '#C8943E' },
    { cssVar: '--hs-green', fallback: '#2D4A2D' },
    { cssVar: '--hs-sand', fallback: '#D4B896' }
  ];

  // Logs a prefixed console warning; extra arguments (canvas, error) are passed through.
  function warn(message, ...details) {
    console.warn('[charts.js] ' + message, ...details);
  }

  // Maps a contract kind to a Chart.js chart type; returns null for unknown kinds.
  function chartTypeFor(kind) {
    switch (kind) {
      case 'line':
        return 'line';
      case 'bar':
      case 'horizontalBar':
        return 'bar';
      default:
        return null;
    }
  }

  // Reads the series colors from the :root CSS variables, falling back to the hex values.
  function readPalette() {
    const styles = window.getComputedStyle(document.documentElement);
    const colors = [];
    for (let i = 0; i < PALETTE.length; i++) {
      const value = styles.getPropertyValue(PALETTE[i].cssVar).trim();
      colors.push(value || PALETTE[i].fallback);
    }
    return colors;
  }

  // True when the user has asked for reduced motion.
  function prefersReducedMotion() {
    return Boolean(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  }

  // Parses and validates a canvas's data-chart JSON; warns and returns null if it is unusable.
  function readSpec(canvas) {
    let spec;
    try {
      spec = JSON.parse(canvas.dataset.chart);
    } catch (err) {
      warn('Skipping chart: data-chart is missing or not valid JSON.', canvas, err);
      return null;
    }
    if (spec === null || typeof spec !== 'object') {
      warn('Skipping chart: data-chart must be a JSON object.', canvas);
      return null;
    }
    if (chartTypeFor(spec.kind) === null) {
      warn('Skipping chart: unknown kind "' + String(spec.kind) + '".', canvas);
      return null;
    }
    if (!Array.isArray(spec.labels) || !Array.isArray(spec.series)) {
      warn('Skipping chart: "labels" and "series" must both be arrays.', canvas);
      return null;
    }
    for (let i = 0; i < spec.series.length; i++) {
      const series = spec.series[i];
      if (series === null || typeof series !== 'object' || !Array.isArray(series.values)) {
        warn('Skipping chart: series ' + i + ' has no "values" array.', canvas);
        return null;
      }
    }
    return spec;
  }

  // Builds the Chart.js dataset for one series, styled for a line or bar chart.
  function buildDataset(type, series, color) {
    const dataset = {
      label: series.name === undefined || series.name === null ? '' : String(series.name),
      data: series.values,
      backgroundColor: color,
      borderColor: color
    };
    if (type === 'line') {
      dataset.fill = false;
      dataset.tension = 0.3;
      dataset.borderWidth = 2;
      dataset.pointBackgroundColor = color;
      dataset.pointBorderColor = color;
    } else {
      dataset.borderRadius = 4;
    }
    return dataset;
  }

  // Assembles the full Chart.js config (type, data, options) from a validated spec.
  function buildConfig(spec, palette, reducedMotion) {
    const type = chartTypeFor(spec.kind);
    const horizontal = spec.kind === 'horizontalBar';

    const datasets = [];
    for (let i = 0; i < spec.series.length; i++) {
      datasets.push(buildDataset(type, spec.series[i], palette[i % palette.length]));
    }

    // Only the value axis is configured; the category axis keeps Chart.js defaults.
    const scales = {};
    scales[horizontal ? 'x' : 'y'] = { beginAtZero: true, ticks: { precision: 0 } };

    const options = {
      responsive: true,
      maintainAspectRatio: false,
      scales: scales,
      plugins: {
        legend: { display: spec.series.length >= 2 }
      }
    };
    if (horizontal) {
      options.indexAxis = 'y';
    }
    if (reducedMotion) {
      options.animation = false;
    }

    return {
      type: type,
      data: { labels: spec.labels, datasets: datasets },
      options: options
    };
  }

  // Draws one canvas's chart, replacing any chart already on it; invalid specs are skipped.
  function renderChart(canvas, palette, reducedMotion) {
    const spec = readSpec(canvas);
    if (spec === null) {
      return;
    }
    const existing = window.Chart.getChart(canvas);
    if (existing) {
      existing.destroy();
    }
    new window.Chart(canvas, buildConfig(spec, palette, reducedMotion));
  }

  // Renders every canvas.js-chart on the page; does nothing if Chart.js is not loaded.
  function renderAll() {
    const canvases = document.querySelectorAll('canvas.js-chart');
    if (canvases.length === 0) {
      return;
    }
    if (typeof window.Chart === 'undefined') {
      warn('Chart.js is not loaded; charts were not rendered.');
      return;
    }
    const palette = readPalette();
    const reducedMotion = prefersReducedMotion();
    for (let i = 0; i < canvases.length; i++) {
      renderChart(canvases[i], palette, reducedMotion);
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', renderAll);
  } else {
    renderAll();
  }
})();

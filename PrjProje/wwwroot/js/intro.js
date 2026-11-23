// Opening animation: loaded synchronously in <head> (home page only) so the overlay is decided before first paint.
// Played on every home page load and skipped for visitors who prefer reduced motion.
(function () {
    "use strict";

    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    document.documentElement.classList.add("intro-play");
    // Kıvılcımlar şimdilik kapalı; açmak için bu satırı geri alın.
    // document.addEventListener("DOMContentLoaded", sparks);

    // Canvas sparks thrown from the laser's cut point, both ways, with gravity and cooling colour.
    function sparks() {
        var intro = document.querySelector(".intro");
        var laser = intro && intro.querySelector(".intro-laser");
        var img = intro && intro.querySelector("img");
        var canvas = document.createElement("canvas");
        var ctx = canvas.getContext && canvas.getContext("2d");
        if (!laser || !img || !ctx) return;

        var dpr = Math.min(window.devicePixelRatio || 1, 2);
        var w = window.innerWidth, h = window.innerHeight;
        canvas.className = "intro-sparks";
        canvas.width = w * dpr;
        canvas.height = h * dpr;
        ctx.scale(dpr, dpr);
        ctx.lineCap = "round";
        intro.appendChild(canvas);

        var parts = [], start = performance.now(), last = start, lastX = null, carry = 0;
        var colors = ["#e30613", "#ff6a2b", "#ff8a3d", "#ffb347", "#ffd27a"];

        function emit(x, top, bottom, count) {
            for (var i = 0; i < count; i++) {
                var ember = Math.random() < 0.12;
                var life = ember ? 0.7 + Math.random() * 0.6 : 0.25 + Math.random() * 0.55;
                parts.push({
                    x: x, y: top + Math.random() * (bottom - top),
                    // Mostly sideways (left and right) and downward, a few kick upward.
                    vx: (Math.random() * 2 - 1) * (ember ? 220 : 520),
                    vy: -Math.random() * 320 + 120,
                    life: life, max: life, ember: ember
                });
            }
        }

        function frame(now) {
            var dt = Math.min((now - last) / 1000, 0.05);
            last = now;
            var beam = laser.getBoundingClientRect(), logo = img.getBoundingClientRect();
            var x = beam.left;
            // Emit only while the beam is actually travelling across the logo.
            var cutting = lastX !== null && x - lastX > 0.2 && x > logo.left && x < logo.right;
            lastX = x;

            ctx.clearRect(0, 0, w, h);

            if (cutting) {
                carry += dt * 650;
                var n = Math.floor(carry);
                carry -= n;
                emit(x, logo.top, logo.bottom, n);

                var cy = (logo.top + logo.bottom) / 2, r = (logo.bottom - logo.top) * 1.1;
                var glow = ctx.createRadialGradient(x, cy, 0, x, cy, r);
                glow.addColorStop(0, "rgba(255, 138, 61, .45)");
                glow.addColorStop(1, "rgba(255, 138, 61, 0)");
                ctx.fillStyle = glow;
                ctx.fillRect(x - r, cy - r, r * 2, r * 2);
            }

            // One path per colour and size (10 strokes a frame instead of one per spark).
            var paths = [];
            for (var b = 0; b < colors.length * 2; b++) paths.push(new Path2D());
            for (var i = parts.length - 1; i >= 0; i--) {
                var p = parts[i];
                p.life -= dt;
                if (p.life <= 0) { parts.splice(i, 1); continue; }
                p.vy += 1500 * dt;
                p.vx *= 1 - 1.6 * dt;
                p.x += p.vx * dt;
                p.y += p.vy * dt;

                var c = Math.min(colors.length - 1, Math.floor(p.life / p.max * colors.length));
                var path = paths[c * 2 + (p.ember ? 1 : 0)];
                path.moveTo(p.x - p.vx * 0.022, p.y - p.vy * 0.022);
                path.lineTo(p.x, p.y);
            }
            for (b = 0; b < paths.length; b++) {
                var shade = b >> 1;
                // Cooler (redder) sparks are also the fading ones.
                ctx.globalAlpha = Math.min(1, (shade + 0.5) / colors.length * 1.6);
                ctx.strokeStyle = colors[shade];
                ctx.lineWidth = b & 1 ? 3 : 1.5;
                ctx.stroke(paths[b]);
            }
            ctx.globalAlpha = 1;

            if (now - start < 2500 && (parts.length || now - start < 1200)) requestAnimationFrame(frame);
            else canvas.remove();
        }
        requestAnimationFrame(frame);
    }
})();

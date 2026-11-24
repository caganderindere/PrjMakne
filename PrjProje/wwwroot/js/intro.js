(function () {
    "use strict";

    // Opening animation: a laser cuts the logo out from right to left. Once per browser session and never
    // with reduced motion. Loaded in <head> (the CSP blocks inline scripts) so the class is set before first paint.
    var root = document.documentElement;
    try {
        if (window.matchMedia("(prefers-reduced-motion: reduce)").matches || sessionStorage.getItem("prj-intro")) {
            return;
        }
        sessionStorage.setItem("prj-intro", "1");
    } catch (e) {
        return;
    }

    root.classList.add("intro");
    var finished = false;
    function finish() {
        if (finished) {
            return;
        }
        finished = true;
        root.classList.add("intro-out");
        window.setTimeout(function () { root.classList.remove("intro", "intro-out"); }, 600);
    }

    window.setTimeout(finish, 2300);
    // A click or Escape skips it.
    document.addEventListener("click", finish, { once: true });
    document.addEventListener("keydown", function (e) { if (e.key === "Escape") { finish(); } });
})();

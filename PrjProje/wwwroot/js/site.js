(function () {
    "use strict";

    document.documentElement.classList.add("js");
    var reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var lang = document.documentElement.lang || "tr";

    // Where hover opens the menu (site.css), a click on the title goes to its first page instead of toggling.
    // Bootstrap's capture-phase handler already cancelled the link, so navigate by hand.
    var hoverMenus = window.matchMedia("(min-width: 1200px) and (hover: hover)");
    document.querySelectorAll(".navbar .nav-item.dropdown > .dropdown-toggle").forEach(function (toggle) {
        toggle.addEventListener("click", function (event) {
            if (!hoverMenus.matches) return;
            if (event.ctrlKey || event.metaKey || event.shiftKey) window.open(toggle.href, "_blank");
            else window.location.href = toggle.href;
        });
    });

    function onVisible(elements, callback, options) {
        if (!("IntersectionObserver" in window)) {
            elements.forEach(callback);
            return;
        }
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    observer.unobserve(entry.target);
                    callback(entry.target);
                }
            });
        }, options || { threshold: 0.2 });
        elements.forEach(function (element) { observer.observe(element); });
    }

    function loadScript(src) {
        return new Promise(function (resolve, reject) {
            var script = document.createElement("script");
            script.src = src;
            script.async = true;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }

    function loadStyle(href) {
        var link = document.createElement("link");
        link.rel = "stylesheet";
        link.href = href;
        document.head.appendChild(link);
    }

    // Header shadow on scroll
    var header = document.querySelector(".site-header");
    if (header) {
        var toggleHeader = function () { header.classList.toggle("is-scrolled", window.scrollY > 10); };
        window.addEventListener("scroll", toggleHeader, { passive: true });
        toggleHeader();
    }

    // Animated counters
    var formatter = new Intl.NumberFormat(lang === "tr" ? "tr-TR" : "en-US");
    var counters = Array.prototype.slice.call(document.querySelectorAll("[data-count]"));
    onVisible(counters, function (element) {
        var target = parseInt(element.getAttribute("data-count"), 10) || 0;
        if (reducedMotion) {
            element.textContent = formatter.format(target);
            return;
        }
        var start = null;
        var duration = 1600;
        function step(timestamp) {
            if (!start) start = timestamp;
            var progress = Math.min((timestamp - start) / duration, 1);
            var eased = 1 - Math.pow(1 - progress, 3);
            element.textContent = formatter.format(Math.round(target * eased));
            if (progress < 1) window.requestAnimationFrame(step);
        }
        window.requestAnimationFrame(step);
    }, { threshold: 0.6 });

    // Reveal-on-scroll for cards
    var revealTargets = Array.prototype.slice.call(document.querySelectorAll(".category-card, .capability-card, .product-card, .blog-card, .process-card, .timeline-item, .principle-card, .info-block"));
    revealTargets.forEach(function (element) { element.classList.add("reveal"); });
    onVisible(revealTargets, function (element) { element.classList.add("is-visible"); }, { threshold: 0.1 });

    // Localized country names
    var countryList = document.querySelector("[data-country-names]");
    if (countryList && window.Intl && Intl.DisplayNames) {
        var names = new Intl.DisplayNames([lang], { type: "region" });
        countryList.querySelectorAll("[data-country]").forEach(function (item) {
            try { item.textContent = names.of(item.getAttribute("data-country")); } catch (e) { /* keep ISO code */ }
        });
    }

    // Export region tabs: show one region's countries; once the map has loaded it also highlights and frames them.
    var onExportRegion = null;
    var regionTabs = document.querySelectorAll(".export-region-tabs [data-region]");
    regionTabs.forEach(function (tab) {
        tab.addEventListener("click", function () {
            var region = tab.getAttribute("data-region"), codes = [];
            regionTabs.forEach(function (other) { other.setAttribute("aria-pressed", other === tab ? "true" : "false"); });
            document.querySelectorAll(".country-chips [data-country]").forEach(function (chip) {
                var match = region === "all" || chip.getAttribute("data-region") === region;
                chip.hidden = !match;
                if (match && region !== "all") codes.push(chip.getAttribute("data-country"));
            });
            if (onExportRegion) onExportRegion(codes);
        });
    });

    // Export world map (loaded only when the section becomes visible)
    var map = document.getElementById("exportMap");
    if (map) {
        onVisible([map], function () {
            var base = "https://cdn.jsdelivr.net/npm/jsvectormap@1.5.3/dist/";
            loadStyle(base + "css/jsvectormap.min.css");
            loadScript(base + "js/jsvectormap.min.js")
                .then(function () { return loadScript(base + "maps/world.js"); })
                .then(function () {
                    var countries = (map.getAttribute("data-countries") || "").split(",").filter(Boolean);
                    map.classList.add("is-loaded");

                    // Wheel zoom only after the visitor clicks the map, so scrolling down the page never gets trapped on it.
                    var wheelEnabled = false, downX = 0, downY = 0;
                    map.addEventListener("mousedown", function (event) { wheelEnabled = true; downX = event.pageX; downY = event.pageY; });
                    map.addEventListener("mouseleave", function () { wheelEnabled = false; });
                    map.addEventListener("wheel", function (event) { if (!wheelEnabled) event.stopImmediatePropagation(); }, { capture: true });

                    var regionNames = window.Intl && Intl.DisplayNames ? new Intl.DisplayNames([lang], { type: "region" }) : null;
                    var vectorMap = new window.jsVectorMap({
                        selector: "#exportMap",
                        map: "world",
                        zoomButtons: false,
                        zoomMax: MAP_ZOOM_MAX,
                        selectedRegions: countries,
                        regionStyle: {
                            initial: { fill: "#e2e4e8", stroke: "#ffffff", strokeWidth: 0.6, fillOpacity: 1 },
                            hover: { fillOpacity: 1, cursor: "inherit" },
                            selected: { fill: "#333741" },
                            selectedHover: { fill: "#d71920", fillOpacity: 1 }
                        },
                        onRegionTooltipShow: function (event, tooltip, code) {
                            try { if (regionNames) tooltip.text(regionNames.of(code)); } catch (e) { /* keep the English name */ }
                        },
                        onRegionClick: function (event, code) {
                            // The library also fires click at the end of a drag.
                            if (Math.abs(event.pageX - downX) + Math.abs(event.pageY - downY) < 5) focusRegion(vectorMap, countries, code);
                        }
                    });
                    countries.forEach(function (code) {
                        var region = vectorMap.regions[code];
                        if (region) region.element.shape.node.classList.add("is-export");
                    });
                    drawRoutes(map, vectorMap, countries);
                })
                .catch(function () { /* map is decorative; placeholder stays visible */ });
        }, { rootMargin: "200px" });
    }

    // Animated export routes: curved arrows from Ankara drawn on an SVG overlay above the map
    var ORIGIN = [39.93, 32.86];
    var CENTROIDS = {
        DE: [51.2, 10.4], FR: [46.6, 2.4], IT: [42.8, 12.6], NL: [52.2, 5.5], BE: [50.6, 4.6], PL: [52.1, 19.4],
        RO: [45.9, 24.9], BG: [42.7, 25.3], GR: [39.1, 22.0], GB: [53.0, -1.5], ES: [40.2, -3.6], PT: [39.6, -8.0],
        RU: [56.0, 40.0], UA: [49.0, 31.4], GE: [42.2, 43.5], AZ: [40.3, 47.7], KZ: [48.0, 67.0], UZ: [41.4, 64.6],
        TM: [39.0, 59.5], KG: [41.5, 74.5], IQ: [33.2, 43.7], SA: [24.0, 45.0], AE: [24.0, 54.0], QA: [25.3, 51.2],
        KW: [29.3, 47.6], JO: [31.2, 36.5], LB: [33.9, 35.9], IR: [32.4, 53.7], EG: [26.8, 30.8], DZ: [28.0, 2.6],
        MA: [31.8, -7.1], LY: [27.0, 17.0], TN: [34.0, 9.5], AT: [47.6, 14.6], CH: [46.8, 8.2], CZ: [49.8, 15.5],
        HU: [47.2, 19.5], RS: [44.0, 20.9], NO: [61.0, 9.0], SE: [62.0, 15.0], FI: [64.0, 26.0], DK: [56.0, 9.5],
        US: [39.8, -98.6], CA: [56.1, -106.3], BR: [-10.8, -52.9], ZA: [-30.6, 22.9], NG: [9.1, 8.7], IN: [22.0, 79.0],
        CN: [35.9, 104.2], PK: [30.4, 69.3], AF: [33.9, 67.7], MN: [46.9, 103.8], AU: [-25.3, 133.8]
    };
    var SVG_NS = "http://www.w3.org/2000/svg";
    var MAP_ZOOM_MAX = 12, MAP_ZOOM_STEP = 1.6;
    // The chart opens on the export markets: this box always fits, whatever shape the map has on the screen.
    var HOME_CENTER = [43, 34], HOME_WEST = [40, -14], HOME_EAST = [40, 82], HOME_NORTH = [62, 34], HOME_SOUTH = [14, 34];

    function focusRegion(vectorMap, countries, code) {
        if (countries.indexOf(code) !== -1 && vectorMap.regions[code]) {
            vectorMap.setFocus({ region: code, animate: !reducedMotion });
        }
    }

    function svg(name, attrs, parent) {
        var element = document.createElementNS(SVG_NS, name);
        Object.keys(attrs || {}).forEach(function (key) { element.setAttribute(key, attrs[key]); });
        if (parent) parent.appendChild(element);
        return element;
    }

    function drawRoutes(container, vectorMap, countries) {
        var overlay = null, group = null, renderScale = 1, renderOrigin = null, homeScale = 1, homeFactor = 1;
        var resetButton = document.querySelector("[data-map-reset]");

        var position = function (coords) { return vectorMap.getMarkerPosition({ coords: coords }); };

        var goHome = function () {
            // The library does not watch the window; without this it keeps measuring against the old size.
            vectorMap.updateSize();
            vectorMap.setFocus({ coords: HOME_CENTER, scale: 1, animate: false });
            var spanX = position(HOME_EAST).x - position(HOME_WEST).x;
            var spanY = position(HOME_SOUTH).y - position(HOME_NORTH).y;
            homeFactor = Math.min(container.offsetWidth / spanX, container.offsetHeight / spanY);
            vectorMap.setFocus({ coords: HOME_CENTER, scale: homeFactor, animate: false });
            homeScale = vectorMap.scale;
        };

        // Routes are drawn once in the pixels of that moment; zoom and pan only move this group.
        var syncRoutes = function () {
            var zoom = vectorMap.scale / renderScale;
            var origin = position(ORIGIN);
            group.setAttribute("transform", "translate(" + (origin.x - renderOrigin.x * zoom) + "," + (origin.y - renderOrigin.y * zoom) + ") scale(" + zoom + ")");
            // Dots and the label keep their on-screen size while the map grows under them.
            group.querySelectorAll("circle[data-r]").forEach(function (circle) { circle.setAttribute("r", circle.getAttribute("data-r") / zoom); });
            group.querySelectorAll("text[data-size]").forEach(function (text) { text.setAttribute("font-size", text.getAttribute("data-size") / zoom); });
            var zoomed = vectorMap.scale > homeScale * 1.01;
            container.classList.toggle("is-zoomed", zoomed);
            resetButton.hidden = !zoomed;
        };

        var dot = function (attrs) {
            attrs["data-r"] = attrs.r;
            return svg("circle", attrs, group);
        };

        var render = function () {
            if (overlay) overlay.remove();
            overlay = svg("svg", { "class": "export-routes", "aria-hidden": "true" });
            container.appendChild(overlay);
            group = svg("g", {}, overlay);

            var origin = position(ORIGIN);
            renderScale = vectorMap.scale;
            renderOrigin = origin;

            // Real meridians and parallels every 15 degrees, so the grid moves with the land.
            var gridLine = function (from, to) {
                var a = position(from), b = position(to);
                svg("line", { x1: a.x, y1: a.y, x2: b.x, y2: b.y, "class": "route-grid" }, group);
            };
            for (var lon = -165; lon <= 165; lon += 15) gridLine([75, lon], [-45, lon]);
            for (var lat = -45; lat <= 75; lat += 15) gridLine([lat, -165], [lat, 165]);

            countries.forEach(function (code, index) {
                var target = CENTROIDS[code];
                if (!target) return;
                var end = position(target);
                var dx = end.x - origin.x, dy = end.y - origin.y;
                var distance = Math.sqrt(dx * dx + dy * dy);
                if (distance < 4) return;
                // Control point lifted perpendicular to the chord so every route arcs upwards.
                var bend = Math.min(distance * 0.3, 90);
                var nx = -dy / distance, ny = dx / distance;
                if (ny > 0) { nx = -nx; ny = -ny; }
                var cx = (origin.x + end.x) / 2 + nx * bend, cy = (origin.y + end.y) / 2 + ny * bend;
                var d = "M" + origin.x + "," + origin.y + " Q" + cx + "," + cy + " " + end.x + "," + end.y;

                var path = svg("path", { d: d, "class": "route-path", "data-route": code, id: "route-" + code }, group);
                var length = path.getTotalLength();
                if (!reducedMotion) {
                    path.style.strokeDasharray = length;
                    path.style.strokeDashoffset = length;
                    path.style.animation = "route-draw 1.1s ease-out " + (0.2 + index * 0.05) + "s forwards";
                    path.addEventListener("animationend", function () { path.style.strokeDasharray = ""; });
                    var flow = svg("path", { d: d, "class": "route-flow", "data-route": code }, group);
                    flow.style.setProperty("--len", length);
                    flow.style.animationDelay = (1.5 + index * 0.27) + "s";
                }
                dot({ cx: end.x, cy: end.y, r: "2.4", "class": "route-target", "data-route": code });
            });

            if (!reducedMotion) dot({ cx: origin.x, cy: origin.y, r: "5", "class": "route-pulse" });
            dot({ cx: origin.x, cy: origin.y, r: "5", "class": "route-origin" });
            svg("text", { x: origin.x, y: origin.y, dx: ".9em", dy: ".35em", "class": "route-label", "data-size": "14" }, group).textContent = "Ankara";
        };

        goHome();
        render();
        syncRoutes();

        // Drag, pinch and each animation step only reach this internal method, not the library's viewport event (version pinned above).
        var applyTransform = vectorMap._applyTransform;
        vectorMap._applyTransform = function () {
            applyTransform.call(vectorMap);
            syncRoutes();
        };

        var resizeTimer, renderedWidth = container.offsetWidth;
        window.addEventListener("resize", function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                // Phones fire resize when the address bar hides; only a real width change needs a redraw.
                if (container.offsetWidth === renderedWidth) return;
                renderedWidth = container.offsetWidth;
                goHome();
                render();
                syncRoutes();
                paint(regionCodes);
            }, 250);
        });

        var chips = {};
        document.querySelectorAll(".country-chips [data-country]").forEach(function (chip) {
            chips[chip.getAttribute("data-country")] = chip;
        });

        // Highlight a route from its country chip or from the country on the map; letting go falls back to the chosen region.
        var regionCodes = [];
        var paint = function (codes) {
            overlay.classList.toggle("has-active", codes.length > 0);
            overlay.querySelectorAll("[data-route]").forEach(function (el) {
                el.classList.toggle("is-active", codes.indexOf(el.getAttribute("data-route")) !== -1);
            });
        };
        var highlight = function (code, on) {
            paint(on ? [code] : regionCodes);
            if (chips[code]) chips[code].classList.toggle("is-active", on);
        };
        onExportRegion = function (codes) {
            regionCodes = codes;
            paint(codes);
            // Türkiye stays in the frame so the routes keep their start in Ankara.
            var regions = codes.concat("TR").filter(function (code) { return vectorMap.regions[code]; });
            if (codes.length) vectorMap.setFocus({ regions: regions, animate: !reducedMotion });
            else vectorMap.setFocus({ coords: HOME_CENTER, scale: homeFactor, animate: !reducedMotion });
        };

        Object.keys(chips).forEach(function (code) {
            var chip = chips[code];
            chip.addEventListener("mouseenter", function () { highlight(code, true); });
            chip.addEventListener("mouseleave", function () { highlight(code, false); });
            chip.addEventListener("focus", function () { highlight(code, true); });
            chip.addEventListener("blur", function () { highlight(code, false); });
            chip.addEventListener("click", function () { focusRegion(vectorMap, countries, code); });
            chip.addEventListener("keydown", function (event) {
                if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    focusRegion(vectorMap, countries, code);
                }
            });
        });

        ["mouseover", "mouseout"].forEach(function (type) {
            container.addEventListener(type, function (event) {
                var code = event.target.getAttribute("data-code");
                if (code && countries.indexOf(code) !== -1) highlight(code, type === "mouseover");
            });
        });

        var controls = document.querySelector("[data-map-controls]");
        controls.hidden = false;
        controls.addEventListener("click", function (event) {
            var button = event.target.closest("button");
            if (!button) return;
            if (button === resetButton) {
                vectorMap.setFocus({ coords: HOME_CENTER, scale: homeFactor, animate: !reducedMotion });
                return;
            }
            var factor = button.getAttribute("data-map-zoom") === "in" ? MAP_ZOOM_STEP : 1 / MAP_ZOOM_STEP;
            vectorMap._setScale(vectorMap.scale * factor, vectorMap._width / 2, vectorMap._height / 2, false, !reducedMotion);
        });
    }

    // References slider: logos drift to the right in an endless loop; a finger, the mouse or keyboard focus takes over
    var referenceSlider = document.querySelector("[data-references-slider]");
    if (referenceSlider && referenceSlider.children.length > 1 && !reducedMotion) {
        var SLIDER_SPEED = 28; // px per second; a negative value would reverse the direction
        var RESUME_DELAY = 1500;
        var originals = Array.prototype.slice.call(referenceSlider.children);
        var cloneSet = function () {
            originals.forEach(function (card) {
                var clone = card.cloneNode(true);
                // Copies only exist for the visual loop: screen readers and the Tab key see each logo once.
                clone.setAttribute("aria-hidden", "true");
                clone.querySelectorAll("a").forEach(function (link) { link.tabIndex = -1; });
                clone.querySelectorAll("img").forEach(function (image) { image.loading = "eager"; });
                referenceSlider.appendChild(clone);
            });
        };
        var setWidth = 0, position = 0, sliderHeld = 0, sliderVisible = false, resumeTimer, lastFrame = 0;
        var measureSlider = function () {
            setWidth = referenceSlider.children[originals.length].offsetLeft - originals[0].offsetLeft;
            // Before layout is ready the width can read 0, which made the clone loop below run forever and freeze iOS Safari.
            if (!(setWidth > 0)) return;
            // One set for each side of the loop, plus enough copies to fill the widest screen this device can show.
            var needed = Math.ceil(Math.max(window.innerWidth, window.screen.width) / setWidth) + 3;
            while (referenceSlider.children.length < originals.length * needed) cloneSet();
            position = setWidth;
            referenceSlider.scrollLeft = position;
        };
        cloneSet();
        measureSlider();
        window.addEventListener("load", measureSlider);
        var wrapPosition = function () {
            if (position < 1) position += setWidth;
            else if (position >= setWidth * 2) position -= setWidth;
        };
        var hold = function () { sliderHeld++; clearTimeout(resumeTimer); };
        var release = function () {
            sliderHeld = Math.max(0, sliderHeld - 1);
            clearTimeout(resumeTimer);
            resumeTimer = setTimeout(function () { position = referenceSlider.scrollLeft; }, RESUME_DELAY - 50);
            lastFrame = performance.now() + RESUME_DELAY;
        };
        // Touch screens fire mouseenter on tap but never mouseleave, which would stop the slider for good.
        if (window.matchMedia("(hover: hover)").matches) {
            referenceSlider.addEventListener("mouseenter", hold);
            referenceSlider.addEventListener("mouseleave", release);
        }
        referenceSlider.addEventListener("focusin", hold);
        referenceSlider.addEventListener("focusout", release);
        referenceSlider.addEventListener("touchstart", hold, { passive: true });
        referenceSlider.addEventListener("touchend", release, { passive: true });
        referenceSlider.addEventListener("touchcancel", release, { passive: true });
        // A manual swipe past either end of the loop continues seamlessly.
        referenceSlider.addEventListener("scroll", function () {
            if (!sliderHeld) return;
            position = referenceSlider.scrollLeft;
            var before = position;
            wrapPosition();
            if (position !== before) referenceSlider.scrollLeft = position;
        }, { passive: true });
        // Phones fire resize when the address bar hides; only a real width change moves the logos.
        var sliderWidth = window.innerWidth;
        window.addEventListener("resize", function () {
            if (window.innerWidth === sliderWidth) return;
            sliderWidth = window.innerWidth;
            measureSlider();
        });
        new IntersectionObserver(function (entries) { sliderVisible = entries[0].isIntersecting; }).observe(referenceSlider);
        var driftSlider = function (now) {
            var elapsed = now - lastFrame;
            if (elapsed > 0) {
                lastFrame = now;
                if (!sliderHeld && sliderVisible) {
                    position -= SLIDER_SPEED * Math.min(elapsed, 100) / 1000;
                    wrapPosition();
                    referenceSlider.scrollLeft = position;
                }
            }
            window.requestAnimationFrame(driftSlider);
        };
        window.requestAnimationFrame(driftSlider);
    }

    // Hero video: fades in once playing; reduced motion keeps it still until the visitor presses play
    var heroVideo = document.querySelector("[data-hero-video]");
    if (heroVideo) {
        var videoToggle = document.querySelector("[data-video-toggle]");
        var setVideoPaused = function (paused) {
            videoToggle.setAttribute("aria-pressed", paused ? "true" : "false");
            videoToggle.setAttribute("aria-label", videoToggle.getAttribute(paused ? "data-label-play" : "data-label-pause"));
        };
        heroVideo.addEventListener("playing", function () { heroVideo.classList.add("is-playing"); setVideoPaused(false); });
        heroVideo.addEventListener("pause", function () { setVideoPaused(true); });
        if (reducedMotion) { heroVideo.removeAttribute("autoplay"); heroVideo.pause(); setVideoPaused(true); }
        videoToggle.addEventListener("click", function () {
            if (heroVideo.paused) { heroVideo.play(); } else { heroVideo.pause(); }
        });
    }

    // Process story: the step crossing the viewport middle becomes active; the rail and stage ring track scroll progress
    var processLayout = document.querySelector("[data-process]");
    if (processLayout) {
        var processTimeline = processLayout.querySelector(".process-timeline");
        var processSteps = Array.prototype.slice.call(processLayout.querySelectorAll(".process-step"));
        var processCount = processLayout.querySelector("[data-process-count]");
        var processTicking = false;
        var processActive = null;

        var updateProcess = function () {
            processTicking = false;
            var middle = window.innerHeight * 0.55;
            var box = processTimeline.getBoundingClientRect();
            var rail = Math.max(0, Math.min(1, (middle - box.top) / box.height));
            processTimeline.style.setProperty("--process-rail", rail.toFixed(3));

            var active = 0;
            processSteps.forEach(function (step, index) {
                if (step.getBoundingClientRect().top < middle) active = index;
            });
            if (active === processActive) return;
            processActive = active;

            processLayout.style.setProperty("--process-progress", ((active + 1) / processSteps.length).toFixed(3));
            processLayout.style.setProperty("--process-active", active);
            processCount.textContent = String(active + 1).padStart(2, "0");
            processLayout.querySelectorAll("[data-step]").forEach(function (element) {
                var index = Number(element.getAttribute("data-step")) - 1;
                element.classList.toggle("is-active", index === active);
                element.classList.toggle("is-past", index < active);
            });
        };
        var requestProcessUpdate = function () {
            if (!processTicking) { processTicking = true; window.requestAnimationFrame(updateProcess); }
        };
        window.addEventListener("scroll", requestProcessUpdate, { passive: true });
        window.addEventListener("resize", requestProcessUpdate);
        updateProcess();
    }

    // Staggered reveal for grids and slide-in visuals
    document.querySelectorAll("[data-stagger]").forEach(function (group) {
        Array.prototype.forEach.call(group.children, function (child, index) {
            var target = child.querySelector(".reveal") || child;
            target.classList.add("reveal");
            target.style.transitionDelay = Math.min(index * 0.08, 0.6) + "s";
        });
    });
    var lateReveal = Array.prototype.slice.call(document.querySelectorAll(".reveal:not(.is-visible), .reveal-left"));
    onVisible(lateReveal, function (element) {
        element.classList.add("is-visible");
        // Clear the stagger delay afterwards so hover transitions stay instant.
        setTimeout(function () { element.style.transitionDelay = ""; }, 1500);
    }, { threshold: 0.1 });

    // Gallery filter
    var filterButtons = document.querySelectorAll("[data-gallery-filter]");
    filterButtons.forEach(function (button) {
        button.addEventListener("click", function () {
            var category = button.getAttribute("data-gallery-filter");
            filterButtons.forEach(function (b) { b.classList.toggle("active", b === button); });
            document.querySelectorAll(".gallery-item").forEach(function (item) {
                item.hidden = category !== "all" && item.getAttribute("data-category") !== category;
            });
        });
    });

    // Product image framing: transparent cut-outs keep the showroom padding; opaque photos fill the frame,
    // which takes the photo's own background colour (plain corners) or is covered edge to edge (busy corners).
    function fitProductImage(frame) {
        var img = frame.querySelector("img");
        if (!img) return;
        function measure() {
            frame.classList.remove("is-photo", "is-photo-cover");
            frame.style.backgroundColor = "";
            try {
                var size = 32, canvas = document.createElement("canvas");
                canvas.width = canvas.height = size;
                var ctx = canvas.getContext("2d", { willReadFrequently: true });
                ctx.drawImage(img, 0, 0, size, size);
                var corners = [[0, 0], [size - 1, 0], [0, size - 1], [size - 1, size - 1]].map(function (p) {
                    return ctx.getImageData(p[0], p[1], 1, 1).data;
                });
                if (corners.some(function (c) { return c[3] < 250; })) return;
                var avg = [0, 1, 2].map(function (k) {
                    return Math.round(corners.reduce(function (s, c) { return s + c[k]; }, 0) / 4);
                });
                var spread = Math.max.apply(null, corners.map(function (c) {
                    return Math.abs(c[0] - avg[0]) + Math.abs(c[1] - avg[1]) + Math.abs(c[2] - avg[2]);
                }));
                frame.classList.add("is-photo");
                if (spread > 60) frame.classList.add("is-photo-cover");
                else frame.style.backgroundColor = "rgb(" + avg.join(",") + ")";
            } catch (e) { /* unreadable (cross-origin) image: keep the default framing */ }
        }
        if (img.complete && img.naturalWidth) measure();
        img.addEventListener("load", measure);
    }
    document.querySelectorAll(".product-card-media, .product-gallery-main").forEach(fitProductImage);

    // Product image thumbnails
    document.querySelectorAll("[data-product-gallery]").forEach(function (gallery) {
        var mainLink = gallery.querySelector("[data-gallery-main]");
        gallery.querySelectorAll("[data-gallery-thumb]").forEach(function (thumb) {
            thumb.addEventListener("click", function () {
                var src = thumb.getAttribute("data-gallery-thumb");
                if (!mainLink) return;
                var image = mainLink.querySelector("img");
                image.removeAttribute("srcset");
                image.src = src;
                mainLink.href = src;
                gallery.querySelectorAll("[data-gallery-thumb]").forEach(function (t) { t.classList.toggle("active", t === thumb); });
            });
        });
    });

    // Lightweight lightbox
    var lightboxLinks = Array.prototype.slice.call(document.querySelectorAll("[data-lightbox]"));
    if (lightboxLinks.length) {
        var overlay = document.createElement("div");
        overlay.className = "lightbox";
        overlay.hidden = true;
        overlay.setAttribute("role", "dialog");
        overlay.setAttribute("aria-modal", "true");
        overlay.innerHTML = '<button type="button" class="lightbox-close" aria-label="Close"><i class="fa-solid fa-xmark"></i></button>' +
            '<button type="button" class="lightbox-prev" aria-label="Previous"><i class="fa-solid fa-chevron-left"></i></button>' +
            '<img alt="" /><button type="button" class="lightbox-next" aria-label="Next"><i class="fa-solid fa-chevron-right"></i></button>' +
            '<div class="lightbox-caption"></div>';
        document.body.appendChild(overlay);

        var image = overlay.querySelector("img");
        var caption = overlay.querySelector(".lightbox-caption");
        var group = [];
        var index = 0;
        var lastFocus = null;

        var show = function (i) {
            index = (i + group.length) % group.length;
            var link = group[index];
            image.src = link.getAttribute("href");
            var text = link.getAttribute("data-caption") || "";
            image.alt = text;
            caption.textContent = text;
        };
        var close = function () {
            overlay.hidden = true;
            document.body.style.overflow = "";
            if (lastFocus) lastFocus.focus();
        };

        lightboxLinks.forEach(function (link) {
            link.addEventListener("click", function (event) {
                event.preventDefault();
                var name = link.getAttribute("data-lightbox");
                group = lightboxLinks.filter(function (l) {
                    return l.getAttribute("data-lightbox") === name && !l.hidden && !(l.closest(".gallery-item") || {}).hidden;
                });
                if (name === "product") {
                    group = lightboxLinks.filter(function (l) { return l.getAttribute("data-lightbox") === name; });
                }
                lastFocus = link;
                show(Math.max(0, group.indexOf(link)));
                overlay.hidden = false;
                document.body.style.overflow = "hidden";
                overlay.querySelector(".lightbox-close").focus();
            });
        });

        overlay.querySelector(".lightbox-close").addEventListener("click", close);
        overlay.querySelector(".lightbox-prev").addEventListener("click", function () { show(index - 1); });
        overlay.querySelector(".lightbox-next").addEventListener("click", function () { show(index + 1); });
        overlay.addEventListener("click", function (event) { if (event.target === overlay) close(); });
        document.addEventListener("keydown", function (event) {
            if (overlay.hidden) return;
            if (event.key === "Escape") close();
            if (event.key === "ArrowLeft") show(index - 1);
            if (event.key === "ArrowRight") show(index + 1);
        });
    }

    // Quote form: keep the hidden product id in sync with the selected product name
    var productInput = document.querySelector("[data-product-input]");
    if (productInput) {
        var hiddenId = productInput.form.querySelector('input[name="Form.ProductId"]');
        productInput.addEventListener("change", function () {
            var match = Array.prototype.find.call(document.querySelectorAll("#productOptions option"), function (option) {
                return option.value === productInput.value;
            });
            if (hiddenId) hiddenId.value = match ? match.getAttribute("data-id") : "";
        });
    }

    // Cookie consent: Google Analytics loads only after the visitor accepts (KVKK requires explicit consent)
    var banner = document.getElementById("cookieBanner");
    if (banner) {
        var consentKey = "prj-analytics-consent";
        var analyticsId = banner.getAttribute("data-analytics-id");
        var consent = null;
        try { consent = window.localStorage.getItem(consentKey); } catch (e) { consent = null; }

        var loadAnalytics = function () {
            if (!analyticsId || window.gtag) return;
            window.dataLayer = window.dataLayer || [];
            window.gtag = function () { window.dataLayer.push(arguments); };
            window.gtag("js", new Date());
            window.gtag("config", analyticsId);
            var script = document.createElement("script");
            script.async = true;
            script.src = "https://www.googletagmanager.com/gtag/js?id=" + encodeURIComponent(analyticsId);
            document.head.appendChild(script);
        };

        var choose = function (value) {
            try { window.localStorage.setItem(consentKey, value); } catch (e) { /* storage unavailable */ }
            banner.hidden = true;
            if (value === "granted") {
                loadAnalytics();
            } else if (window.gtag) {
                // Withdrawn consent: remove the GA cookies (set on the bare domain) and reload without the script.
                var host = window.location.hostname.replace(/^www\./, "");
                document.cookie.split(";").forEach(function (cookie) {
                    var name = cookie.split("=")[0].trim();
                    if (name.indexOf("_ga") !== 0) return;
                    ["", "; domain=" + host, "; domain=." + host].forEach(function (domain) {
                        document.cookie = name + "=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/" + domain;
                    });
                });
                window.location.reload();
            }
        };

        if (consent === "granted") loadAnalytics();
        else if (consent !== "denied") banner.hidden = false;
        banner.querySelector("[data-cookie-accept]").addEventListener("click", function () { choose("granted"); });
        banner.querySelector("[data-cookie-reject]").addEventListener("click", function () { choose("denied"); });
        document.querySelectorAll("[data-cookie-settings]").forEach(function (link) {
            link.addEventListener("click", function (event) {
                event.preventDefault();
                banner.hidden = false;
            });
        });
    }

    // Announcements (top bar / pop-up): once closed, stay closed until the announcement is edited
    var dismissedKey = "prj-dismissed-announcements";
    var dismissed = [];
    try { dismissed = JSON.parse(window.localStorage.getItem(dismissedKey) || "[]"); } catch (e) { dismissed = []; }
    function dismiss(id) {
        if (dismissed.indexOf(id) === -1) dismissed.push(id);
        try { window.localStorage.setItem(dismissedKey, JSON.stringify(dismissed.slice(-20))); } catch (e) { /* storage unavailable */ }
    }

    var announcementBar = document.querySelector(".announcement-bar[data-announcement]");
    if (announcementBar) {
        var barId = announcementBar.getAttribute("data-announcement");
        if (dismissed.indexOf(barId) !== -1) {
            announcementBar.remove();
        } else {
            announcementBar.querySelector("[data-announcement-close]").addEventListener("click", function () {
                dismiss(barId);
                announcementBar.remove();
            });
        }
    }

    var popup = document.querySelector("[data-popup]");
    if (popup && dismissed.indexOf(popup.getAttribute("data-announcement")) === -1) {
        var popupId = popup.getAttribute("data-announcement");
        var popupBox = popup.querySelector(".announcement-popup-box");
        var lastFocus = null;
        var closePopup = function () {
            dismiss(popupId);
            popup.classList.add("is-closing");
            setTimeout(function () { popup.hidden = true; }, reducedMotion ? 0 : 180);
            document.removeEventListener("keydown", onKey);
            if (lastFocus) lastFocus.focus();
        };
        var onKey = function (event) {
            if (event.key === "Escape") closePopup();
            if (event.key !== "Tab") return;
            // Keep keyboard focus inside the dialog while it is open.
            var focusable = popup.querySelectorAll("a[href], button");
            var first = focusable[0], last = focusable[focusable.length - 1];
            if (event.shiftKey && (document.activeElement === first || document.activeElement === popupBox)) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        };
        popup.querySelectorAll("[data-announcement-close]").forEach(function (element) {
            element.addEventListener("click", closePopup);
        });
        // Wait for the opening animation (when it plays) so the pop-up is not hidden behind it.
        var delay = document.documentElement.classList.contains("intro-play") ? 2200 : 900;
        setTimeout(function () {
            lastFocus = document.activeElement;
            popup.hidden = false;
            document.addEventListener("keydown", onKey);
            // The dialog itself takes focus: on the close button it would look pressed from the first moment.
            popupBox.focus();
        }, delay);
    }

    // WhatsApp button: count the click for the admin dashboard. A beacon survives the tab switching to WhatsApp.
    // Product page "ask marketing" bubble: appears after a short delay, stays closed for the session once dismissed.
    var bubble = document.querySelector("[data-whatsapp-bubble]");
    var bubbleKey = "prj-whatsapp-bubble";
    var bubbleClosed = false;
    try { bubbleClosed = window.sessionStorage.getItem(bubbleKey) === "1"; } catch (e) { /* storage unavailable */ }
    if (bubble && !bubbleClosed) {
        setTimeout(function () { bubble.hidden = false; }, 4000);
        bubble.querySelector("[data-whatsapp-bubble-close]").addEventListener("click", function () {
            bubble.hidden = true;
            try { window.sessionStorage.setItem(bubbleKey, "1"); } catch (e) { /* storage unavailable */ }
        });
    }

    var whatsApp = document.querySelector(".whatsapp-float");
    if (whatsApp && navigator.sendBeacon) {
        whatsApp.addEventListener("click", function () { navigator.sendBeacon("/api/whatsapp-click"); });
    }
})();

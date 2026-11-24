// Page behaviour: product gallery, image lightbox with zoom, gallery filters and click-to-load YouTube videos.
(function () {
    "use strict";

    // Product gallery: thumbnails swap the main image.
    document.querySelectorAll("[data-gallery]").forEach(function (gallery) {
        var main = gallery.querySelector("[data-gallery-main]");
        var mainImage = main && main.querySelector("img");
        gallery.querySelectorAll("[data-gallery-thumb]").forEach(function (thumb) {
            thumb.addEventListener("click", function () {
                mainImage.src = thumb.getAttribute("data-src");
                mainImage.srcset = thumb.getAttribute("data-srcset") || "";
                mainImage.alt = thumb.getAttribute("data-alt") || "";
                main.setAttribute("data-index", thumb.getAttribute("data-index"));
                gallery.querySelectorAll("[data-gallery-thumb]").forEach(function (t) { t.setAttribute("aria-current", String(t === thumb)); });
            });
        });
    });

    // Lightbox for any [data-lightbox] group; the main gallery button opens the image it currently shows.
    var dialog = document.querySelector("[data-lightbox-dialog]");
    if (dialog && dialog.showModal) {
        var stage = dialog.querySelector(".lightbox-stage");
        var image = stage.querySelector("img");
        var caption = dialog.querySelector(".lightbox-caption");
        var items = [];
        var index = 0;

        var show = function (i) {
            index = (i + items.length) % items.length;
            stage.classList.remove("is-zoomed");
            image.src = items[index].href;
            image.alt = items[index].alt;
            caption.textContent = items[index].caption;
        };

        var open = function (group, start) {
            items = Array.prototype.map.call(document.querySelectorAll('[data-lightbox="' + group + '"]'), function (el) {
                return { href: el.getAttribute("data-full") || el.getAttribute("href"), alt: el.getAttribute("data-alt") || "", caption: el.getAttribute("data-caption") || "" };
            }).filter(function (item, i, all) { return item.href && all.findIndex(function (o) { return o.href === item.href; }) === i; });
            if (items.length === 0) { return; }
            dialog.querySelectorAll("[data-lightbox-prev], [data-lightbox-next]").forEach(function (b) { b.hidden = items.length < 2; });
            show(Math.max(0, items.findIndex(function (item) { return item.href === start; })));
            dialog.showModal();
        };

        document.addEventListener("click", function (event) {
            var trigger = event.target.closest("[data-lightbox]");
            if (!trigger || trigger.closest("[hidden]")) { return; }
            event.preventDefault();
            open(trigger.getAttribute("data-lightbox"), trigger.getAttribute("data-full") || trigger.getAttribute("href"));
        });

        image.addEventListener("click", function () { stage.classList.toggle("is-zoomed"); });
        dialog.querySelector("[data-lightbox-close]").addEventListener("click", function () { dialog.close(); });
        dialog.querySelector("[data-lightbox-prev]").addEventListener("click", function () { show(index - 1); });
        dialog.querySelector("[data-lightbox-next]").addEventListener("click", function () { show(index + 1); });
        dialog.addEventListener("keydown", function (event) {
            if (event.key === "ArrowLeft") { show(index - 1); }
            if (event.key === "ArrowRight") { show(index + 1); }
        });
    }

    // Gallery category filter.
    document.querySelectorAll("[data-filter-group]").forEach(function (group) {
        var target = document.getElementById(group.getAttribute("data-filter-group"));
        group.querySelectorAll("button").forEach(function (button) {
            button.addEventListener("click", function () {
                var value = button.getAttribute("data-filter");
                group.querySelectorAll("button").forEach(function (b) { b.setAttribute("aria-pressed", String(b === button)); });
                target.querySelectorAll("[data-category]").forEach(function (item) {
                    item.hidden = value === "all" ? item.hasAttribute("data-repeat") : item.getAttribute("data-category") !== value;
                });
            });
        });
    });

    // YouTube: the player (and its cookies) loads only after the visitor clicks.
    document.addEventListener("click", function (event) {
        var button = event.target.closest("[data-youtube]");
        if (!button) { return; }
        var frame = document.createElement("iframe");
        frame.src = "https://www.youtube-nocookie.com/embed/" + encodeURIComponent(button.getAttribute("data-youtube")) + "?autoplay=1&rel=0";
        frame.title = button.getAttribute("data-title") || "YouTube";
        frame.allow = "accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture";
        frame.allowFullscreen = true;
        var wrapper = document.createElement("div");
        wrapper.className = "video";
        wrapper.appendChild(frame);
        button.replaceWith(wrapper);
    });

    var reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var canObserve = "IntersectionObserver" in window && !reducedMotion;

    // Scroll reveal: only blocks that start below the fold are hidden, so nothing seen on load blinks out.
    if (canObserve) {
        var revealObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) { return; }
                revealObserver.unobserve(entry.target);
                entry.target.classList.remove("reveal-pending");
                // Drop the stagger delay once the items are in, or it would also delay their hover effects.
                setTimeout(function () {
                    Array.prototype.forEach.call(entry.target.children, function (child) { child.style.removeProperty("--reveal-index"); });
                }, 1400);
            });
        }, { rootMargin: "0px 0px -10% 0px" });
        document.querySelectorAll("[data-reveal]").forEach(function (element) {
            if (element.getBoundingClientRect().top < window.innerHeight) { return; }
            if (element.getAttribute("data-reveal") === "items") {
                Array.prototype.forEach.call(element.children, function (child, i) { child.style.setProperty("--reveal-index", String(i)); });
            }
            element.classList.add("reveal-pending");
            revealObserver.observe(element);
        });
    }


})();

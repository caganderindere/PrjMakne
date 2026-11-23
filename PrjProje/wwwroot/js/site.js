(function () {
    "use strict";

    var reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var desktop = window.matchMedia("(min-width: 1180px)");

    // Header: compact once the page scrolls
    var header = document.querySelector("[data-header]");
    if (header) {
        var onScroll = function () { header.classList.toggle("is-compact", window.scrollY > 24); };
        onScroll();
        window.addEventListener("scroll", onScroll, { passive: true });
    }

    // Off-canvas navigation (below the desktop breakpoint)
    var nav = document.querySelector("[data-nav]");
    var openButton = document.querySelector("[data-nav-open]");
    var backdrop = document.querySelector(".nav-backdrop");
    function setNav(open) {
        document.body.classList.toggle("nav-open", open);
        if (openButton) openButton.setAttribute("aria-expanded", open ? "true" : "false");
        if (backdrop) backdrop.hidden = !open;
        if (open) {
            var first = nav.querySelector("button, a");
            if (first) first.focus();
        } else if (openButton && nav.contains(document.activeElement)) {
            openButton.focus();
        }
    }
    if (nav && openButton) {
        openButton.addEventListener("click", function () { setNav(true); });
        document.querySelectorAll("[data-nav-close]").forEach(function (element) {
            element.addEventListener("click", function () { setNav(false); });
        });
        desktop.addEventListener("change", function () { setNav(false); });
    }

    // Submenus: the chevron button toggles; on desktop hover and keyboard focus also open them (site.css).
    var toggles = document.querySelectorAll(".sub-toggle");
    function closeSubmenus(except) {
        toggles.forEach(function (toggle) {
            if (toggle !== except) toggle.setAttribute("aria-expanded", "false");
        });
    }
    toggles.forEach(function (toggle) {
        toggle.addEventListener("click", function () {
            var open = toggle.getAttribute("aria-expanded") !== "true";
            if (desktop.matches) closeSubmenus(toggle);
            toggle.setAttribute("aria-expanded", open ? "true" : "false");
        });
    });

    // Dropdowns built on <details> (language menu): close on an outside click
    var dropdowns = document.querySelectorAll("details[data-dropdown]");
    document.addEventListener("click", function (event) {
        dropdowns.forEach(function (details) {
            if (details.open && !details.contains(event.target)) details.open = false;
        });
        if (desktop.matches && !event.target.closest(".nav-item")) closeSubmenus(null);
    });

    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") return;
        if (document.body.classList.contains("nav-open")) setNav(false);
        closeSubmenus(null);
        dropdowns.forEach(function (details) {
            if (details.open) {
                details.open = false;
                details.querySelector("summary").focus();
            }
        });
    });

    // Quote form: keep the hidden product id in sync with the product name picked from the list
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
        document.querySelectorAll("[data-cookie-settings]").forEach(function (button) {
            button.addEventListener("click", function () { banner.hidden = false; });
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
        var onKey = function (event) {
            if (event.key === "Escape") closePopup();
            if (event.key !== "Tab") return;
            // Keep keyboard focus inside the dialog while it is open.
            var focusable = popup.querySelectorAll("a[href], button");
            var first = focusable[0], last = focusable[focusable.length - 1];
            if (event.shiftKey && (document.activeElement === first || document.activeElement === popupBox)) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        };
        var closePopup = function () {
            dismiss(popupId);
            popup.classList.add("is-closing");
            setTimeout(function () { popup.hidden = true; }, reducedMotion ? 0 : 180);
            document.removeEventListener("keydown", onKey);
            if (lastFocus) lastFocus.focus();
        };
        popup.querySelectorAll("[data-announcement-close]").forEach(function (element) {
            element.addEventListener("click", closePopup);
        });
        setTimeout(function () {
            lastFocus = document.activeElement;
            popup.hidden = false;
            document.addEventListener("keydown", onKey);
            // The dialog itself takes focus: on the close button it would look pressed from the first moment.
            popupBox.focus();
        }, 900);
    }

    // WhatsApp links: count the click for the admin dashboard. A beacon survives the tab switching to WhatsApp.
    if (navigator.sendBeacon) {
        document.querySelectorAll("[data-whatsapp]").forEach(function (link) {
            link.addEventListener("click", function () { navigator.sendBeacon("/api/whatsapp-click"); });
        });
    }
})();

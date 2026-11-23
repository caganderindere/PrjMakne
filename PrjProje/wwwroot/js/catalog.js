// Product catalog viewer: renders the PDF with pdf.js and turns its pages like a magazine (StPageFlip).
// Both libraries are only downloaded when a visitor opens a catalog.
(function () {
    "use strict";

    var viewer = document.getElementById("catalogViewer");
    if (!viewer) return;

    var PDFJS = "https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/";
    var FLIP = "https://cdn.jsdelivr.net/npm/page-flip@2.0.7/dist/js/page-flip.browser.js";
    var stage = viewer.querySelector(".catalog-viewer-stage");
    var status = viewer.querySelector(".catalog-viewer-status");
    var counter = viewer.querySelector(".catalog-viewer-page");
    var download = viewer.querySelector("[data-catalog-download]");
    var libs = null, session = null;

    function script(src) {
        return new Promise(function (resolve, reject) {
            var el = document.createElement("script");
            el.src = src;
            el.onload = resolve;
            el.onerror = reject;
            document.head.appendChild(el);
        });
    }

    function loadLibraries() {
        if (!libs) {
            libs = Promise.all([script(PDFJS + "pdf.min.js"), script(FLIP)]).then(function () {
                window.pdfjsLib.GlobalWorkerOptions.workerSrc = PDFJS + "pdf.worker.min.js";
            });
            libs.catch(function () { libs = null; });
        }
        return libs;
    }

    function setStatus(text) {
        status.textContent = text || "";
        status.hidden = !text;
    }

    async function open(url, title) {
        viewer.querySelector("#catalogViewerTitle").textContent = title;
        download.href = url;
        counter.textContent = "";
        setStatus(viewer.dataset.loading);
        viewer.showModal();

        var current = session = { pdf: null, flip: null, book: null };
        try {
            await loadLibraries();
            var pdf = await window.pdfjsLib.getDocument(url).promise;
            if (session !== current) { pdf.destroy(); return; }
            current.pdf = pdf;
            await build(current, pdf);
            setStatus("");
        } catch (e) {
            if (session === current) setStatus(viewer.dataset.error);
        }
    }

    async function build(current, pdf) {
        var first = (await pdf.getPage(1)).getViewport({ scale: 1 });
        var ratio = first.height / first.width;
        var room = stage.getBoundingClientRect();
        var spread = room.width >= 768;
        var maxW = (room.width - (spread ? 128 : 16)) / (spread ? 2 : 1);
        var maxH = room.height - 32;
        // shortcut: sized once on open, a later resize or rotation keeps the old size until the catalog is reopened.
        var pageW = Math.floor(Math.min(maxW, maxH / ratio));
        var pageH = Math.floor(pageW * ratio);
        var count = pdf.numPages;

        var book = current.book = document.createElement("div");
        book.className = "catalog-book";
        book.style.width = pageW * (spread ? 2 : 1) + "px";
        book.style.height = pageH + "px";
        stage.appendChild(book);

        var pages = [], rendered = [];
        for (var i = 0; i < count; i++) {
            var page = document.createElement("div");
            page.className = "catalog-page";
            if (i === 0 || i === count - 1) page.dataset.density = "hard";
            book.appendChild(page);
            pages.push(page);
        }

        var dpr = Math.min(window.devicePixelRatio || 1, 2);
        function render(index) {
            if (index < 0 || index >= count || rendered[index]) return;
            rendered[index] = true;
            pdf.getPage(index + 1).then(function (p) {
                var viewport = p.getViewport({ scale: pageW * dpr / p.getViewport({ scale: 1 }).width });
                var canvas = document.createElement("canvas");
                canvas.width = Math.floor(viewport.width);
                canvas.height = Math.floor(viewport.height);
                pages[index].appendChild(canvas);
                return p.render({ canvasContext: canvas.getContext("2d"), viewport: viewport }).promise;
            }).catch(function () { rendered[index] = false; });
        }
        function around(index) {
            for (var j = index - 2; j <= index + 3; j++) render(j);
            counter.textContent = (index + 1) + " / " + count;
        }

        var flip = current.flip = new window.St.PageFlip(book, {
            width: pageW, height: pageH, size: "fixed",
            // No corner curl on hover; pages still turn with the animation from the buttons, arrow keys or a drag.
            showCover: true, usePortrait: true, mobileScrollSupport: false, showPageCorners: false,
            maxShadowOpacity: 0.45, flippingTime: 700
        });
        flip.loadFromHTML(pages);
        flip.on("flip", function (e) { around(e.data); });
        around(0);
    }

    function cleanup() {
        var current = session;
        session = null;
        if (!current) return;
        if (current.flip) current.flip.destroy();
        else if (current.book) current.book.remove();
        if (current.pdf) current.pdf.destroy();
    }

    document.addEventListener("click", function (e) {
        var trigger = e.target.closest("[data-catalog]");
        if (trigger) open(trigger.dataset.catalog, trigger.dataset.title);
    });
    viewer.querySelector("[data-catalog-close]").addEventListener("click", function () { viewer.close(); });
    viewer.querySelector("[data-catalog-prev]").addEventListener("click", function () { if (session && session.flip) session.flip.flipPrev(); });
    viewer.querySelector("[data-catalog-next]").addEventListener("click", function () { if (session && session.flip) session.flip.flipNext(); });
    viewer.addEventListener("keydown", function (e) {
        if (!session || !session.flip) return;
        if (e.key === "ArrowLeft") session.flip.flipPrev();
        if (e.key === "ArrowRight") session.flip.flipNext();
    });
    viewer.addEventListener("close", cleanup);
})();

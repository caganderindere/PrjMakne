(function () {
    "use strict";

    // Mobile sidebar
    document.querySelectorAll("[data-sidebar-toggle]").forEach(function (element) {
        element.addEventListener("click", function () { document.body.classList.toggle("sidebar-open"); });
    });

    // Confirmation for destructive actions (no inline handlers because of the CSP)
    document.addEventListener("submit", function (event) {
        var message = event.target.getAttribute("data-confirm");
        if (message && !window.confirm(message)) {
            event.preventDefault();
        }
    });

    // Slug preview: fills an empty slug from its source field
    var slugMap = { "ç": "c", "ğ": "g", "ı": "i", "İ": "i", "ö": "o", "ş": "s", "ü": "u", "Ç": "c", "Ğ": "g", "Ö": "o", "Ş": "s", "Ü": "u" };
    function slugify(text) {
        return text.replace(/[çğıİöşüÇĞÖŞÜ]/g, function (c) { return slugMap[c]; })
            .normalize("NFD").replace(/[̀-ͯ]/g, "")
            .toLowerCase().replace(/[^a-z0-9\s-]/g, " ").trim()
            .replace(/\s+/g, "-").replace(/-+/g, "-");
    }
    document.querySelectorAll("[data-slug-source]").forEach(function (source) {
        var target = document.getElementById(source.getAttribute("data-slug-source"));
        if (!target) return;
        var touched = target.value.length > 0;
        target.addEventListener("input", function () { touched = target.value.length > 0; });
        source.addEventListener("input", function () {
            if (!touched) target.value = slugify(source.value);
        });
    });

    // Character counters for SEO fields
    document.querySelectorAll("[data-counter]").forEach(function (input) {
        var max = parseInt(input.getAttribute("maxlength") || input.getAttribute("data-counter"), 10);
        var counter = document.createElement("div");
        counter.className = "form-text text-end";
        input.insertAdjacentElement("afterend", counter);
        var update = function () {
            counter.textContent = input.value.length + " / " + max;
            counter.classList.toggle("text-danger", input.value.length > max);
        };
        input.addEventListener("input", update);
        update();
    });

    // Image preview before upload
    document.querySelectorAll("input[type=file][data-preview]").forEach(function (input) {
        var preview = document.getElementById(input.getAttribute("data-preview"));
        input.addEventListener("change", function () {
            if (!preview || !input.files || !input.files[0]) return;
            preview.src = URL.createObjectURL(input.files[0]);
            preview.hidden = false;
        });
    });

    // Home page sections (Site Settings): reorder with the arrow buttons, dim hidden ones
    document.querySelectorAll("[data-sortable-list]").forEach(function (list) {
        list.addEventListener("click", function (event) {
            var button = event.target.closest("[data-move]");
            if (!button) return;
            var item = button.closest("li");
            if (button.getAttribute("data-move") === "up" && item.previousElementSibling) {
                list.insertBefore(item, item.previousElementSibling);
            } else if (button.getAttribute("data-move") === "down" && item.nextElementSibling) {
                list.insertBefore(item.nextElementSibling, item);
            }
            button.focus();
        });
        list.addEventListener("change", function (event) {
            if (event.target.matches("input[type=checkbox]")) {
                event.target.closest("li").classList.toggle("is-hidden", !event.target.checked);
            }
        });
    });

    // Restricted category warning on the product form
    var categorySelect = document.querySelector("[data-restricted-ids]");
    if (categorySelect) {
        var restricted = (categorySelect.getAttribute("data-restricted-ids") || "").split(",").filter(Boolean);
        var warning = document.getElementById("restrictedWarning");
        var toggleWarning = function () { if (warning) warning.hidden = restricted.indexOf(categorySelect.value) === -1; };
        categorySelect.addEventListener("change", toggleWarning);
        toggleWarning();
    }

    // Announcement form: live preview of the bar or pop-up, in either language, plus a plain sentence about when it goes live
    var announcementForm = document.querySelector("[data-announcement-form]");
    if (announcementForm) {
        var previewLang = "Tr";
        var field = function (id) { return document.getElementById(id); };
        var fillAll = function (selector, text) {
            announcementForm.querySelectorAll(selector).forEach(function (element) { element.textContent = text; });
        };
        var previewImage = field("announcementPreview");
        var removeImage = field("RemoveImage");
        var previewStatus = announcementForm.querySelector("[data-preview-status]");

        var describeSchedule = function () {
            var start = new Date(field("StartDate").value);
            var end = field("EndDate").value ? new Date(field("EndDate").value) : null;
            var format = function (date) { return date.toLocaleString("tr-TR", { dateStyle: "medium", timeStyle: "short" }); };
            if (!field("IsActive").checked) return { ok: false, text: "Pasif: kaydedilse de sitede görünmez." };
            if (end && end <= start) return { ok: false, text: "Bitiş tarihi başlangıçtan sonra olmalı." };
            if (end && end <= new Date()) return { ok: false, text: "Bitiş tarihi geçmiş: sitede görünmez." };
            var until = end ? format(end) + " tarihine kadar yayında kalır." : "Süresiz yayında kalır.";
            if (start > new Date()) return { ok: true, text: format(start) + " tarihinde yayına girer. " + until };
            return { ok: true, text: "Kaydedince yayına girer. " + until };
        };

        var updatePreview = function () {
            var isPopup = field("displayPopup").checked;
            var linkText = field("LinkText" + previewLang).value.trim() || (previewLang === "Tr" ? "Detaylı bilgi" : "Learn more");
            var hasLink = field("LinkUrl").value.trim().length > 0;
            announcementForm.querySelector("[data-preview-bar]").hidden = isPopup;
            announcementForm.querySelector("[data-preview-popup]").hidden = !isPopup;
            announcementForm.querySelector("[data-popup-only]").hidden = !isPopup;
            announcementForm.querySelector("[data-schedule-step]").textContent = isPopup ? "5" : "4";
            fillAll("[data-preview-text]", field("Message" + previewLang).value.trim() || "Duyuru metniniz burada görünecek.");
            fillAll("[data-preview-label]", previewLang === "Tr" ? "Duyuru" : "Announcement");
            fillAll("[data-preview-link]", hasLink ? linkText : "");
            announcementForm.querySelectorAll("[data-preview-link]").forEach(function (link) { link.hidden = !hasLink; });
            if (removeImage) previewImage.hidden = removeImage.checked || !previewImage.getAttribute("src");
            var schedule = describeSchedule();
            previewStatus.textContent = schedule.text;
            previewStatus.className = "ann-status " + (schedule.ok ? "is-ok" : "is-warning");
        };

        announcementForm.addEventListener("input", updatePreview);
        announcementForm.addEventListener("change", updatePreview);
        announcementForm.querySelectorAll("[data-preview-lang]").forEach(function (button) {
            button.addEventListener("click", function () {
                previewLang = button.getAttribute("data-preview-lang");
                announcementForm.querySelectorAll("[data-preview-lang]").forEach(function (other) {
                    other.classList.toggle("active", other === button);
                    other.setAttribute("aria-pressed", other === button ? "true" : "false");
                });
                updatePreview();
            });
        });
        updatePreview();
    }

    // Developer page: opens every sitemap page like an anonymous visitor and reports failures and slow pages
    var pageScan = document.querySelector("[data-page-scan]");
    if (pageScan) {
        var SLOW_MS = 1500, TIMEOUT_MS = 15000, PARALLEL = 4, SLOWEST_SHOWN = 5;
        var scanStart = pageScan.querySelector("[data-scan-start]");
        var scanBar = pageScan.querySelector("[data-scan-bar]");
        var scanSummary = pageScan.querySelector("[data-scan-summary]");
        var scanTable = pageScan.querySelector("[data-scan-table]");

        var checkPage = function (path) {
            var started = performance.now();
            return fetch(path, { credentials: "omit", cache: "no-store", headers: { "X-Page-Scan": "1" }, signal: AbortSignal.timeout(TIMEOUT_MS) })
                .then(function (response) { return { path: path, status: response.status, ok: response.ok, ms: Math.round(performance.now() - started) }; })
                .catch(function () { return { path: path, status: "Yanıt yok", ok: false, ms: Math.round(performance.now() - started) }; });
        };

        var showScanResults = function (results) {
            var failed = results.filter(function (r) { return !r.ok; });
            var slow = results.filter(function (r) { return r.ok && r.ms > SLOW_MS; });
            var average = results.length ? Math.round(results.reduce(function (sum, r) { return sum + r.ms; }, 0) / results.length) : 0;
            scanSummary.textContent = results.length + " sayfa tarandı · " + failed.length + " hatalı · " + slow.length + " yavaş · ortalama " + average + " ms";
            scanSummary.classList.toggle("text-danger", failed.length > 0);

            var slowest = results.filter(function (r) { return r.ok; }).sort(function (a, b) { return b.ms - a.ms; }).slice(0, SLOWEST_SHOWN);
            var body = scanTable.querySelector("tbody");
            body.textContent = "";
            failed.concat(slowest).forEach(function (r) {
                var row = body.insertRow();
                var link = document.createElement("a");
                link.href = r.path;
                link.target = "_blank";
                link.textContent = decodeURI(r.path);
                row.insertCell().appendChild(link);
                var badge = document.createElement("span");
                badge.className = "badge " + (!r.ok ? "bg-danger" : r.ms > SLOW_MS ? "bg-warning text-dark" : "bg-success");
                badge.textContent = r.status;
                row.insertCell().appendChild(badge);
                var time = row.insertCell();
                time.className = "text-end small text-nowrap";
                time.textContent = r.ms + " ms";
            });
            scanTable.hidden = body.rows.length === 0;
        };

        var runScan = function () {
            scanStart.disabled = true;
            scanBar.style.width = "0";
            scanTable.hidden = true;
            scanSummary.classList.remove("text-danger");
            scanSummary.textContent = "Site haritası okunuyor…";
            fetch("/sitemap.xml", { credentials: "omit", cache: "no-store" })
                .then(function (response) {
                    if (!response.ok) throw new Error(response.status);
                    return response.text();
                })
                .then(function (xml) {
                    var doc = new DOMParser().parseFromString(xml, "application/xml");
                    var urls = Array.prototype.map.call(doc.getElementsByTagName("loc"), function (loc) { return loc.textContent; })
                        .concat(Array.prototype.map.call(doc.getElementsByTagNameNS("*", "link"), function (link) { return link.getAttribute("href"); }));
                    // Only the path is used: Site:BaseUrl may name another host than the one being checked.
                    var paths = Array.from(new Set(urls.filter(Boolean).map(function (url) { return new URL(url, location.origin).pathname; })));
                    var results = [];
                    var next = 0;
                    var worker = function () {
                        if (next >= paths.length) return Promise.resolve();
                        return checkPage(paths[next++]).then(function (result) {
                            results.push(result);
                            scanBar.style.width = (results.length / paths.length * 100) + "%";
                            scanSummary.textContent = results.length + " / " + paths.length + " sayfa tarandı…";
                            return worker();
                        });
                    };
                    var workers = [];
                    for (var i = 0; i < PARALLEL; i++) workers.push(worker());
                    return Promise.all(workers).then(function () { showScanResults(results); });
                })
                .catch(function (error) {
                    scanSummary.textContent = "Site haritası okunamadı (" + error.message + ").";
                    scanSummary.classList.add("text-danger");
                })
                .finally(function () { scanStart.disabled = false; });
        };

        scanStart.addEventListener("click", runScan);
        runScan();
    }

    // Developer page: performance charts; Chart.js is loaded only here
    var perfRoot = document.querySelector("[data-performance]");
    if (perfRoot) {
        var PRIMARY = "#2563eb", SECONDARY = "#c2410c", GRID = "rgba(0, 0, 0, .06)", MUTED = "#6c757d";
        var perfMessage = perfRoot.querySelector("[data-perf-message]");
        var perfRanges = perfRoot.querySelectorAll("[data-perf-range]");
        var perfCharts = {};
        var perfRequest = 0;

        var setStat = function (name, value) {
            perfRoot.querySelector('[data-perf-stat="' + name + '"]').textContent = value;
        };
        var number = function (value) { return Number(value).toLocaleString("tr-TR"); };
        var withUnit = function (value, unit) { return value === null ? "-" : number(value) + " " + unit; };

        var line = function (label, data, color) {
            return { type: "line", label: label, data: data, borderColor: color, backgroundColor: color, borderWidth: 2, pointRadius: 2, pointHoverRadius: 4, spanGaps: false };
        };

        var drawChart = function (name, labels, datasets) {
            if (perfCharts[name]) perfCharts[name].destroy();
            perfCharts[name] = new window.Chart(perfRoot.querySelector('[data-perf-chart="' + name + '"]'), {
                data: { labels: labels, datasets: datasets },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    animation: false,
                    interaction: { mode: "index", intersect: false },
                    plugins: { legend: { display: datasets.length > 1, align: "end", labels: { boxWidth: 12, boxHeight: 2, color: MUTED } } },
                    scales: {
                        x: { grid: { display: false }, ticks: { color: MUTED, maxTicksLimit: 8, maxRotation: 0 } },
                        y: { beginAtZero: true, grid: { color: GRID }, border: { display: false }, ticks: { color: MUTED, maxTicksLimit: 5 } }
                    }
                }
            });
        };

        var showRoutes = function (routes) {
            var body = perfRoot.querySelector("[data-perf-routes]");
            body.textContent = "";
            if (!routes.length) {
                var empty = body.insertRow().insertCell();
                empty.colSpan = 5;
                empty.className = "text-center text-muted py-4";
                empty.textContent = "Henüz veri yok.";
                return;
            }
            routes.forEach(function (route) {
                var row = body.insertRow();
                row.insertCell().textContent = "/" + route.route;
                [number(route.requests), withUnit(route.averageMs, "ms"), route.p95Ms === null ? "-" : "≤ " + number(route.p95Ms) + " ms", "%" + number(route.errorPercent)].forEach(function (text) {
                    var cell = row.insertCell();
                    cell.className = "text-end small text-nowrap";
                    cell.textContent = text;
                });
            });
        };

        var showPerformance = function (report) {
            var labels = report.points.map(function (p) { return p.label; });
            var series = function (key) { return report.points.map(function (p) { return p[key]; }); };

            perfMessage.textContent = report.totalRequests === 0 ? "Bu aralıkta henüz veri yok. Toplama yeni başladıysa veriler birkaç dakika içinde görünür." : "";
            perfMessage.classList.remove("text-danger");
            setStat("requests", number(report.totalRequests));
            setStat("average", withUnit(report.averageMs, "ms"));
            setStat("p95", report.p95Ms === null ? "-" : "≤ " + number(report.p95Ms) + " ms");
            setStat("errors", report.serverErrorPercent === null ? "-" : "%" + number(report.serverErrorPercent));
            setStat("memory", withUnit(report.memoryMb, "MB"));
            setStat("uptime", report.uptime);

            drawChart("latency", labels, [line("Ortalama", series("averageMs"), PRIMARY), line("En yavaş %5", series("p95Ms"), SECONDARY)]);
            drawChart("traffic", labels, [{ type: "bar", label: "İstek", data: series("requests"), backgroundColor: PRIMARY, borderRadius: { topLeft: 4, topRight: 4 }, borderSkipped: "bottom", maxBarThickness: 18 }]);
            drawChart("errors", labels, [line("4xx", series("clientErrorPercent"), PRIMARY), line("5xx", series("serverErrorPercent"), SECONDARY)]);
            drawChart("memory", labels, [line("Bellek", series("memoryMb"), PRIMARY)]);
            showRoutes(report.slowestRoutes);
        };

        var loadPerformance = function (days) {
            var request = ++perfRequest;
            perfRanges.forEach(function (button) {
                var active = button.getAttribute("data-perf-range") === String(days);
                button.classList.toggle("btn-dark", active);
                button.classList.toggle("btn-outline-dark", !active);
                button.setAttribute("aria-pressed", active);
            });
            fetch(perfRoot.getAttribute("data-url") + "?days=" + days, { cache: "no-store" })
                .then(function (response) {
                    if (!response.ok) throw new Error(response.status);
                    return response.json();
                })
                .then(function (report) { if (request === perfRequest) showPerformance(report); })
                .catch(function (error) {
                    if (request !== perfRequest) return;
                    perfMessage.textContent = "Performans verileri alınamadı (" + error.message + ").";
                    perfMessage.classList.add("text-danger");
                });
        };

        var chartScript = document.createElement("script");
        chartScript.src = "https://cdn.jsdelivr.net/npm/chart.js@4.4.6/dist/chart.umd.js";
        chartScript.onload = function () {
            perfRanges.forEach(function (button) {
                button.addEventListener("click", function () { loadPerformance(Number(button.getAttribute("data-perf-range"))); });
            });
            loadPerformance(1);
        };
        chartScript.onerror = function () {
            perfMessage.textContent = "Grafik kütüphanesi yüklenemedi.";
            perfMessage.classList.add("text-danger");
        };
        document.body.appendChild(chartScript);
    }

    // Rich text editor (Quill) loaded only on pages that need it
    var editors = document.querySelectorAll("textarea[data-rich-editor]");
    if (editors.length) {
        var base = "https://cdn.jsdelivr.net/npm/quill@2.0.2/dist/";
        var css = document.createElement("link");
        css.rel = "stylesheet";
        css.href = base + "quill.snow.css";
        document.head.appendChild(css);

        var script = document.createElement("script");
        script.src = base + "quill.js";
        script.onload = function () {
            editors.forEach(function (textarea) {
                var holder = document.createElement("div");
                holder.className = "rich-editor";
                textarea.insertAdjacentElement("afterend", holder);
                textarea.hidden = true;
                var quill = new window.Quill(holder, {
                    theme: "snow",
                    modules: {
                        toolbar: [
                            [{ header: [2, 3, false] }],
                            ["bold", "italic", "underline"],
                            [{ list: "ordered" }, { list: "bullet" }],
                            ["blockquote", "link", "image"],
                            ["clean"]
                        ]
                    }
                });
                if (textarea.value) quill.clipboard.dangerouslyPasteHTML(textarea.value);
                textarea.form.addEventListener("submit", function () {
                    var html = quill.root.innerHTML;
                    textarea.value = html === "<p><br></p>" ? "" : html;
                });
            });
        };
        document.body.appendChild(script);
    }
})();

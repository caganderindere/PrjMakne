// Product search type-ahead: after a short pause, shows the five most relevant products under (or above) the box.
// Arrow keys move through the list, Enter opens the highlighted product (or submits the search), Escape closes it.
(function () {
    "use strict";
    document.querySelectorAll("[data-product-suggest]").forEach(function (form) {
        var input = form.querySelector("input[type=search]");
        var list = form.querySelector("[role=listbox]");
        var endpoint = form.getAttribute("data-product-suggest");
        var timer = 0;
        var request = null;
        var active = -1;

        function options() { return list.querySelectorAll("[role=option]"); }

        function close() {
            list.hidden = true;
            list.innerHTML = "";
            input.setAttribute("aria-expanded", "false");
            input.removeAttribute("aria-activedescendant");
            active = -1;
        }

        function highlight(index) {
            var items = options();
            if (!items.length) return;
            active = (index + items.length) % items.length;
            items.forEach(function (item, i) { item.setAttribute("aria-selected", i === active ? "true" : "false"); });
            input.setAttribute("aria-activedescendant", items[active].id);
        }

        function render(products) {
            if (!products.length) { close(); return; }
            list.innerHTML = "";
            products.forEach(function (product, i) {
                var li = document.createElement("li");
                li.id = list.id + "-" + i;
                li.setAttribute("role", "option");
                li.setAttribute("aria-selected", "false");
                var link = document.createElement("a");
                link.href = product.url;
                link.tabIndex = -1;
                var media = document.createElement("span");
                media.className = "products-suggest-media";
                if (product.image) {
                    var img = document.createElement("img");
                    img.src = product.image;
                    img.alt = "";
                    img.width = 48;
                    img.height = 48;
                    media.appendChild(img);
                }
                var text = document.createElement("span");
                var name = document.createElement("strong");
                name.textContent = product.name;
                var category = document.createElement("small");
                category.textContent = product.categoryName;
                text.append(name, category);
                link.append(media, text);
                li.appendChild(link);
                li.addEventListener("mousedown", function (e) { e.preventDefault(); }); // keep focus so the click lands
                list.appendChild(li);
            });
            list.hidden = false;
            input.setAttribute("aria-expanded", "true");
            active = -1;
        }

        function lookup() {
            var term = input.value.trim();
            if (term.length < 2) { close(); return; }
            if (request) request.abort();
            request = new AbortController();
            fetch(endpoint + "&q=" + encodeURIComponent(term), { signal: request.signal, headers: { Accept: "application/json" } })
                .then(function (response) { return response.ok ? response.json() : []; })
                // Drop answers for text the user has already changed.
                .then(function (products) { if (input.value.trim() === term) render(products); })
                .catch(function () { /* aborted or offline: the normal search still works */ });
        }

        input.addEventListener("input", function () {
            clearTimeout(timer);
            timer = setTimeout(lookup, 180);
        });

        input.addEventListener("keydown", function (e) {
            if (list.hidden) return;
            if (e.key === "ArrowDown") { e.preventDefault(); highlight(active + 1); }
            else if (e.key === "ArrowUp") { e.preventDefault(); highlight(active - 1); }
            else if (e.key === "Escape") { close(); }
            else if (e.key === "Enter" && active >= 0) {
                e.preventDefault();
                window.location.href = options()[active].querySelector("a").href;
            }
        });

        input.addEventListener("blur", close);
    });
})();

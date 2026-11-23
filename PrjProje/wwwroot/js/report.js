// Printable panel documents (_ReportLayout): the toolbar's print button. A script file, because the CSP blocks inline handlers.
document.querySelectorAll("[data-print]").forEach((button) => button.addEventListener("click", () => window.print()));

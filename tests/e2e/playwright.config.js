// End-to-end checks against a freshly seeded local copy of the site.
// Run: cd tests/e2e && npm install && npx playwright install chromium && npx playwright test
const { defineConfig } = require("@playwright/test");
const path = require("path");

const port = process.env.PRJ_E2E_PORT || "5090";

module.exports = defineConfig({
    testDir: ".",
    timeout: 60_000,
    workers: 4,
    reporter: [["list"]],
    use: { baseURL: `http://localhost:${port}`, browserName: "chromium" },
    webServer: {
        // A throw-away database, so the seed content is what the tests see.
        command: `dotnet run --project "${path.resolve(__dirname, "../../PrjProje")}" --no-launch-profile --urls http://localhost:${port}`,
        url: `http://localhost:${port}/tr`,
        timeout: 600_000,
        reuseExistingServer: true,
        env: {
            ASPNETCORE_ENVIRONMENT: "Development",
            ConnectionStrings__DefaultConnection:
                process.env.PRJ_E2E_DB ||
                "Server=(localdb)\\MSSQLLocalDB;Database=PrjE2E;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
        },
    },
});

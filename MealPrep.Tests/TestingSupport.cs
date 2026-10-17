using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Bunit;
using MealPrep.Models;
using MealPrep.Pages;
using MealPrep.Services;
using Microsoft.Extensions.DependencyInjection;



// Testing support classes for MealPrep.Tests. These are not tests themselves, but helpers used by the test classes.
// Provides a fake HTTP backend, test data builders, and a base class for Home page tests that sets up services and common actions.
// Uses a fake HttpClient to simulate responses for items.json and deals.json without making real HTTP requests.
// Provides reusable helper methods for interacting with the Home page in tests, such as logging in, seeding spending history, and clicking buttons.
// This file helps reduce code duplication across test classes and makes it easier to write focused tests for the MealPrep application.
namespace MealPrep.Tests
{
    // Fake HTTP backend shared by every test class (replaces the private handler
    // that was copy-pasted into each file). Serves items.json and, optionally,
    // deals.json. Anything else is a 404, like a missing file in the real app.
    public class FakeAppHandler : HttpMessageHandler
    {
        private readonly string _itemsJson;
        private readonly string? _dealsJson;

        public FakeAppHandler(string itemsJson, string? dealsJson = null)
        {
            _itemsJson = itemsJson;
            _dealsJson = dealsJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";

            if (path.EndsWith("items.json"))
                return Task.FromResult(JsonResponse(_itemsJson));

            if (path.EndsWith("deals.json") && _dealsJson is not null)
                return Task.FromResult(JsonResponse(_dealsJson));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage JsonResponse(string json) =>
            new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    // Builds items.json content for tests. Every item has a single "Generic"
    // supermarket price, like the placeholder data used elsewhere.
    public static class TestData
    {
        public static string Item(int id, string name, decimal price, string emoji = "🍎") =>
            $"{{\"Id\":{id},\"Name\":\"{name}\",\"Category\":\"Fruit\"," +
            $"\"Prices\":[{{\"Supermarket\":\"Generic\",\"Price\":{price.ToString(CultureInfo.InvariantCulture)}}}]," +
            $"\"Emoji\":\"{emoji}\"}}";

        public static string Items(params string[] items) => "[" + string.Join(",", items) + "]";

        public static readonly string Apple = Item(1, "Apple", 0.50m, "🍎");
        public static readonly string Banana = Item(2, "Banana", 0.30m, "🍌");
    }

    // Base class for every test that renders <Home>. Registers the services the
    // page needs, so each test class only describes its own scenario.
    public abstract class HomeTestBase : BunitContext
    {
        protected const string DefaultEmail = "tester@example.com";

        protected HomeTestBase()
        {
            UseBackend(TestData.Items(TestData.Apple));   // default data: one Apple at $0.50

            JSInterop.Mode = JSRuntimeMode.Loose;          // localStorage calls succeed without a browser
            Services.AddBlazoredLocalStorage();
            Services.AddScoped<AuthService>();
            Services.AddScoped<SpendingHistoryService>();
        }

        // Replaces the fake items/deals data. Must be called before anything is
        // resolved from Services (LogInAsync) or Render<Home>() runs.
        protected void UseBackend(string itemsJson, string? dealsJson = null)
        {
            var client = new HttpClient(new FakeAppHandler(itemsJson, dealsJson))
            {
                BaseAddress = new Uri("http://localhost/")
            };
            Services.AddSingleton(client);
        }

        // Signs a user up and logs them in, so Home sees AuthService.CurrentUser.
        // localStorage is a no-op in tests, so this only lives in memory.
        protected async Task LogInAsync(string email = DefaultEmail)
        {
            var auth = Services.GetRequiredService<AuthService>();
            var error = new ErrorBox();
            await auth.SignUpAsync("Test User", email, "password1", error);
            await auth.LoginAsync(email, "password1", error);
        }

        // Makes localStorage return a saved spending history for this user.
        // Totals are oldest first. The key mirrors SpendingHistoryService.KeyFor
        // (private there), so update this if that key format ever changes.
        protected void SeedSpendingHistory(string email, params decimal[] totalsOldestFirst)
        {
            var records = totalsOldestFirst
                .Select((total, i) => new SpendingRecord
                {
                    Date = DateTime.Now.AddDays(-(totalsOldestFirst.Length - i)),
                    Total = total
                })
                .ToList();

            var key = $"mealprep_spending_{email.ToLowerInvariant()}";
            JSInterop.Setup<string>("localStorage.getItem", key).SetResult(JsonSerializer.Serialize(records));
        }

        // Re-finds the element on every call so a re-render can't leave us holding a stale one.
        protected static Task ClickAsync(IRenderedComponent<Home> cut, string selector, int index = 0) =>
            cut.InvokeAsync(() => cut.FindAll(selector)[index].Click());

        protected static async Task SetBudgetAsync(IRenderedComponent<Home> cut, string value)
        {
            cut.Find("#budget").Input(value);
            await ClickAsync(cut, ".set-budget-btn");
        }

        // Clicks "Add" on the first item card.
        protected static Task AddFirstItemAsync(IRenderedComponent<Home> cut) =>
            ClickAsync(cut, "button.add-btn");
    }
}
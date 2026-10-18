using System.Globalization;
using System.Net;
using System.Text;
using Blazored.LocalStorage;
using Bunit;
using MealPrep.Layout;
using MealPrep.Models;
using MealPrep.Pages;
using MealPrep.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// All automated test cases for MealPrep, in one file.
// Each test method starts with the TC ID used in the Final Project Report (Task 3),
// so the code, the test case table and the traceability matrix all use the same numbers.
//
//   TC-001 to TC-004  FR5, FR6  Cart
//   TC-005, TC-006    FR10      Budget input
//   TC-007 to TC-010  FR11      Over-budget pop-up
//   TC-011            FR6       Search
//   TC-016            FR3       Suggested budget
//   TC-017            FR4       Deal notifications
//   TC-018            FR5       GST breakdown
//   TC-024            NFR5      Text size accessibility setting
namespace MealPrep.Tests
{
    public class MealPrepTests : BunitContext
    {
        // ------------------------------------------------------------------
        // Test setup
        // ------------------------------------------------------------------

        public MealPrepTests()
        {
            UseBackend(TestData.Items(TestData.Apple));   // default data: one Apple at $0.50

            JSInterop.Mode = JSRuntimeMode.Loose;          // localStorage calls succeed without a browser
            Services.AddBlazoredLocalStorage();
            Services.AddScoped<AuthService>();
            Services.AddScoped<SpendingHistoryService>();
            Services.AddScoped<AccessibilitySettings>();
        }

        // ------------------------------------------------------------------
        // TC-001 to TC-004 | FR5, FR6 | Cart
        // ------------------------------------------------------------------

        // TC-001 | FR5, FR6 | Positive
        [Fact]
        public async Task TC001_AddSingleItem_ShowsNameQtyPriceAndRemove()
        {
            var cut = Render<Home>();

            await AddFirstItemAsync(cut);

            Assert.Single(cut.FindAll(".cart-item-row"));
            Assert.Contains("Apple", cut.Find(".cart-item-info .item-name").TextContent);
            Assert.Contains("Qty: 1", cut.Find(".cart-item-qty").TextContent);
            Assert.Contains("$0.50", cut.Find(".cart-item-info .item-price").TextContent);
            Assert.NotNull(cut.Find(".remove-btn"));
        }

        // TC-002 | FR5, FR6 | Positive
        [Fact]
        public async Task TC002_AddSameItemTwice_ShowsQtyTwoInOneRow()
        {
            var cut = Render<Home>();

            await AddFirstItemAsync(cut);
            await AddFirstItemAsync(cut);

            Assert.Single(cut.FindAll(".cart-item-row"));
            Assert.Contains("Qty: 2", cut.Find(".cart-item-qty").TextContent);
        }

        // TC-003 | FR5, FR6 | Positive
        [Fact]
        public async Task TC003_AddTwoDifferentItems_ShowsTwoRowsAndSummedTotal()
        {
            UseBackend(TestData.Items(TestData.Apple, TestData.Banana));
            var cut = Render<Home>();

            await ClickAsync(cut, "button.add-btn", 0);   // Apple  $0.50
            await ClickAsync(cut, "button.add-btn", 1);   // Banana $0.30

            Assert.Equal(2, cut.FindAll(".cart-item-row").Count);
            Assert.Contains("$0.80", cut.Find(".total-line-main").TextContent);
        }

        // TC-004 | FR5, FR6 | Positive
        [Fact]
        public async Task TC004_RemoveOneOfTwoItems_KeepsOtherAndLowersTotal()
        {
            UseBackend(TestData.Items(TestData.Apple, TestData.Banana));
            var cut = Render<Home>();
            await ClickAsync(cut, "button.add-btn", 0);   // Apple  $0.50
            await ClickAsync(cut, "button.add-btn", 1);   // Banana $0.30

            await ClickAsync(cut, ".remove-btn", 0);      // remove Apple

            var rows = cut.FindAll(".cart-item-row");
            Assert.Single(rows);
            Assert.Contains("Banana", rows[0].TextContent);
            Assert.Contains("$0.30", cut.Find(".total-line-main").TextContent);   // dropped Apple's $0.50
        }

        // ------------------------------------------------------------------
        // TC-005, TC-006 | FR10 | Budget input
        // ------------------------------------------------------------------

        // TC-005 | FR10 | Positive
        [Fact]
        public async Task TC005_SetValidBudget_ShowsBudgetAndLookingGood()
        {
            var cut = Render<Home>();

            await SetBudgetAsync(cut, "100");
            await AddFirstItemAsync(cut);

            Assert.Contains("$100.00", cut.Find(".budget-amount").TextContent);
            Assert.Contains("Looking good", cut.Find(".budget-status-label").TextContent);
        }

        // TC-006 | FR10 | Invalid input (runs once for each of the 4 inputs)
        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("0")]
        [InlineData("-10")]
        public async Task TC006_InvalidBudget_ClearsBudgetAndShowsNoBudgetSet(string invalidInput)
        {
            var cut = Render<Home>();
            await SetBudgetAsync(cut, "100");
            Assert.Contains("$100.00", cut.Find(".budget-amount").TextContent);   // budget is set before the invalid input

            await SetBudgetAsync(cut, invalidInput);

            Assert.Contains("Budget: —", cut.Find(".budget-amount").TextContent);
            Assert.Contains("No budget set", cut.Find(".budget-status-label").TextContent);
        }

        // ------------------------------------------------------------------
        // TC-007 to TC-010 | FR11 | Over-budget pop-up
        // ------------------------------------------------------------------

        // TC-007 | FR11 | Boundary / negative
        [Fact]
        public async Task TC007_AddItemOverBudget_ShowsModalWithNameAndNewTotal()
        {
            var cut = await OpenOverBudgetPromptAsync();

            var modal = cut.Find(".modal-box");
            Assert.Contains("Apple", modal.TextContent);
            Assert.Contains("$1.50", modal.TextContent);
            Assert.Contains("over your $1.00 budget", modal.TextContent);
            Assert.Equal(3, cut.FindAll(".modal-actions button").Count);   // Add anyway / Cancel / Change budget
        }

        // TC-008 | FR11 | Positive
        [Fact]
        public async Task TC008_ModalAddAnyway_ClosesModalAddsItemAndUpdatesTotal()
        {
            var cut = await OpenOverBudgetPromptAsync();

            await ClickAsync(cut, ".modal-actions button", 0);   // Add anyway

            Assert.Empty(cut.FindAll(".modal-box"));
            Assert.Contains("Qty: 3", cut.Find(".cart-item-qty").TextContent);
            Assert.Contains("$1.50", cut.Find(".total-line-main").TextContent);
        }

        // TC-009 | FR11 | Negative
        [Fact]
        public async Task TC009_ModalCancel_ClosesModalAndLeavesCartUnchanged()
        {
            var cut = await OpenOverBudgetPromptAsync();

            await ClickAsync(cut, ".modal-actions button", 1);   // Cancel

            Assert.Empty(cut.FindAll(".modal-box"));
            Assert.Contains("Qty: 2", cut.Find(".cart-item-qty").TextContent);
            Assert.Contains("$1.00", cut.Find(".total-line-main").TextContent);
        }

        // TC-010 | FR11 | Positive
        [Fact]
        public async Task TC010_ModalChangeBudget_ClosesModalLeavesCartAndBudgetEditable()
        {
            var cut = await OpenOverBudgetPromptAsync();

            await ClickAsync(cut, ".modal-actions button", 2);   // Change budget

            Assert.Empty(cut.FindAll(".modal-box"));
            Assert.Contains("Qty: 2", cut.Find(".cart-item-qty").TextContent);
            Assert.Contains("$1.00", cut.Find(".total-line-main").TextContent);   // total unchanged

            await SetBudgetAsync(cut, "5");                      // the user can now edit the budget
            Assert.Contains("$5.00", cut.Find(".budget-amount").TextContent);
        }

        // ------------------------------------------------------------------
        // TC-011 | FR6 | Search
        // ------------------------------------------------------------------

        // TC-011 | FR6 | Negative
        [Fact]
        public void TC011_SearchWithNoMatch_ShowsNoItemsFoundMessage()
        {
            var cut = Render<Home>();

            cut.Find(".search-box").Input("zzzznonexistentitem");

            Assert.Contains("No items found", cut.Find(".empty-msg").TextContent);
        }

        // ------------------------------------------------------------------
        // TC-016 | FR3 | Suggested budget
        // ------------------------------------------------------------------

        // TC-016 | FR3 | Boundary
        [Fact]
        public void TC016_SixTrips_UsesOnlyTheNewestFive()
        {
            var service = new SpendingHistoryService(null!);   // only the calculation is used, no storage
            var history = new List<SpendingRecord>
            {
                Trip(6, 100m),   // oldest: must be ignored (the average would be $25 if it counted)
                Trip(5, 10m), Trip(4, 10m), Trip(3, 10m), Trip(2, 10m), Trip(1, 10m)
            };

            Assert.Equal(10.00m, service.GetSuggestedBudget(history));
        }

        // ------------------------------------------------------------------
        // TC-017 | FR4 | Deal notifications
        // ------------------------------------------------------------------

        // TC-017 | FR4 | Error handling (deals.json missing)
        [Fact]
        public async Task TC017_MissingDealsFile_PageStillWorksAndShowsNoDeals()
        {
            // No deals file: the fake server answers 404 for deals.json.
            UseBackend(TestData.Items(TestData.Apple), dealsJson: null);

            // The bell is shown in MainLayout's header, so Home is rendered inside the layout.
            var cut = RenderInLayout(typeof(Home));

            Assert.NotEmpty(cut.FindAll(".item-card"));        // the rest of the page loaded
            Assert.Empty(cut.FindAll(".notif-count"));

            await cut.InvokeAsync(() => cut.Find(".notif-bell").Click());
            Assert.Contains("No deals right now", cut.Find(".notif-panel").TextContent);
        }

        // ------------------------------------------------------------------
        // TC-018 | FR5 | GST breakdown
        // ------------------------------------------------------------------

        // TC-018 | FR5 | Positive
        [Fact]
        public async Task TC018_SingleItem_ShowsInclusiveExclusiveAndGst()
        {
            var cut = Render<Home>();

            await AddFirstItemAsync(cut);

            Assert.Contains("Total (incl. GST): $0.50", cut.Find(".total-line-main").TextContent);
            Assert.Contains("Excl. GST: $0.43", cut.Find(".total-line-sub").TextContent);
            Assert.Contains("GST: $0.07", cut.Find(".total-line-sub").TextContent);
        }

        // ------------------------------------------------------------------
        // TC-024 | NFR5 | Text size accessibility setting
        // ------------------------------------------------------------------

        // TC-024 | NFR5 | Accessibility / boundary
        [Fact]
        public void TC024_TextSizeButtons_ChangeLevelAndRespectLimits()
        {
            // The accessibility controls live in MainLayout, so Home is rendered inside the layout.
            var cut = RenderInLayout(typeof(Home));
            Assert.NotEmpty(cut.FindAll(".app-root.text-0"));
            Assert.True(cut.Find(".text-smaller").HasAttribute("disabled"));

            cut.Find(".text-larger").Click();
            Assert.NotEmpty(cut.FindAll(".app-root.text-1"));

            cut.Find(".text-larger").Click();
            Assert.NotEmpty(cut.FindAll(".app-root.text-2"));
            Assert.True(cut.Find(".text-larger").HasAttribute("disabled"));
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        // Replaces the fake items/deals data. Call it before Render runs.
        private void UseBackend(string itemsJson, string? dealsJson = null)
        {
            var client = new HttpClient(new FakeAppHandler(itemsJson, dealsJson))
            {
                BaseAddress = new Uri("http://localhost/")
            };
            Services.AddSingleton(client);
        }

        // Renders a page inside MainLayout, which holds the header (deals bell) and accessibility settings.
        private IRenderedComponent<MainLayout> RenderInLayout(Type pageType) =>
            Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b =>
            {
                b.OpenComponent(0, pageType);
                b.CloseComponent();
            })));

        // Finds the element again on every call so a re-render can't leave us holding an old one.
        private static Task ClickAsync(IRenderedComponent<Home> cut, string selector, int index = 0) =>
            cut.InvokeAsync(() => cut.FindAll(selector)[index].Click());

        private static async Task SetBudgetAsync(IRenderedComponent<Home> cut, string value)
        {
            cut.Find("#budget").Input(value);
            await ClickAsync(cut, ".set-budget-btn");
        }

        // Clicks "Add" on the first item card.
        private static Task AddFirstItemAsync(IRenderedComponent<Home> cut) =>
            ClickAsync(cut, "button.add-btn");

        // Sets a $1.00 budget and adds three $0.50 Apples, so the third add opens the over-budget pop-up.
        private async Task<IRenderedComponent<Home>> OpenOverBudgetPromptAsync()
        {
            var cut = Render<Home>();
            await SetBudgetAsync(cut, "1");

            await AddFirstItemAsync(cut);
            await AddFirstItemAsync(cut);
            Assert.Empty(cut.FindAll(".modal-box"));      // total == budget is not "over"

            await AddFirstItemAsync(cut);                 // $1.50 > $1.00
            return cut;
        }

        private static SpendingRecord Trip(int daysAgo, decimal total) =>
            new() { Date = new DateTime(2026, 10, 1).AddDays(-daysAgo), Total = total };
    }

    // Fake server for the tests. Serves items.json and (optionally) deals.json;
    // anything else is a 404, like a missing file in the real app.
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

    // Small, known item lists so expected totals can be worked out by hand.
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
}

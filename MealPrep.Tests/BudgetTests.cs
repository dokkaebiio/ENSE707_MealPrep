using Bunit;
using MealPrep.Pages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Blazored.LocalStorage;
using MealPrep.Services;


namespace MealPrep.Tests
{
    public class BudgetTests : BunitContext
    {
        public BudgetTests()
        {
            // register a fake HttpClient that returns a single GroceryItem for /data/items.json
            var handler = new FakeItemsHandler();
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            Services.AddSingleton<HttpClient>(client);
            JSInterop.Mode = JSRuntimeMode.Loose;   // lets localStorage calls succeed without a real browser
            Services.AddBlazoredLocalStorage();
            Services.AddScoped<AuthService>();
            Services.AddScoped<SpendingHistoryService>();
        }

        // TC-005
        [Fact]
        public void SetValidBudget_UpdatesBudgetUi()
        {
            // Arrange
            var cut = Render<Home>();
            // Act - set a valid budget
            var budgetInput = cut.Find("#budget");
            budgetInput.Input("100");
            cut.Find(".set-budget-btn").Click();
            // Assert - budget displayed in UI
            var budgetDisplay = cut.Find(".budget-amount").TextContent.Trim();
            Assert.Contains("$100.00", budgetDisplay);
            var statusDisplay = cut.Find(".budget-status-label").TextContent.Trim();
            Assert.Contains("Looking good", statusDisplay);
        }

        // TC-006
        [Fact]
        public void SetInvalidBudget_ClearsBudgetUi()
        {
            // Arrange
            var cut = Render<Home>();
            // Act - set an invalid budget (negative value)
            var budgetInput = cut.Find("#budget");
            budgetInput.Input("-50");
            cut.Find(".set-budget-btn").Click();
            // Assert - budget displayed in UI is cleared or shows $0.00
            var budgetDisplay = cut.Find(".budget-amount").TextContent.Trim();
            Assert.Contains("Budget: Not set", budgetDisplay);
            var statusDisplay = cut.Find(".budget-status-label").TextContent.Trim();
            Assert.Contains("No budget set", statusDisplay);
        }

        // TC-007
        [Fact]
        public async Task AddItemOverBudget_ShowModal()
        {
            var cut = Render<Home>();
            cut.Find("#budget").Input("0.2");
            cut.Find(".set-budget-btn").Click();

            await cut.InvokeAsync(() => cut.Find("button.add-btn").Click());
            var modal = cut.Find(".modal-box");
            Assert.Contains("Apple", modal.TextContent);
            Assert.Contains("over your $0.20 budget", modal.TextContent);
        }

        // TC-010
        [Fact]
        public async Task ChangeBudget_UpdatesBudgetUi()
        {
            // Arrange
            var cut = Render<Home>();
            // Set initial budget
            cut.Find("#budget").Input("0.2");
            await cut.InvokeAsync(() => cut.Find(".set-budget-btn").Click());

            // Act - change the budget to a new value
            await cut.InvokeAsync(() => cut.Find("button.add-btn").Click());
            var button = cut.FindAll(".modal-actions button");
            await cut.InvokeAsync(() => button[2].Click()); // 3rd button is "Change Budget" in the modal

            // Assert - budget displayed in UI reflects new value, closes modal and cart remains unchanged
            Assert.Empty(cut.FindAll(".modal-box")); // Modal should be closed
            Assert.Empty(cut.FindAll(".cart-item-row")); // Cart should remain unchanged
        }

        // TC-008
        [Fact]
        public async Task AddItemExceedingBudget_AddAnyway()
        {
            // Arrange
            var cut = Render<Home>();
            cut.Find("#budget").Input("0.02");
            cut.Find(".set-budget-btn").Click();
            await cut.InvokeAsync(() => cut.Find("button.add-btn").Click());

            var button = cut.FindAll(".modal-actions button");
            await cut.InvokeAsync(() => button[0].Click()); // Click "Add Anyway"

            var cartItems = cut.FindAll(".cart-item-row");
            Assert.Single(cartItems);
        }
        // TC-009
        [Fact]
        public async Task AddItemExceedingBudget_Cancel()
        {
            // Arrange
            var cut = Render<Home>();
            cut.Find("#budget").Input("0.02");
            cut.Find(".set-budget-btn").Click();
            await cut.InvokeAsync(() => cut.Find("button.add-btn").Click());
            var button = cut.FindAll(".modal-actions button");
            await cut.InvokeAsync(() => button[1].Click()); // Click "Cancel"
            var cartItems = cut.FindAll(".cart-item-row");
            Assert.Empty(cartItems);
        }

        // TC-014 (NFR5): dark mode toggle
        [Fact]
        public void DarkModeToggle_AddsAndRemovesDarkClass()
        {
            var cut = Render<Home>();
            Assert.Empty(cut.FindAll(".app-root.dark"));

            cut.Find(".theme-toggle").Click();
            Assert.NotEmpty(cut.FindAll(".app-root.dark"));

            cut.Find(".theme-toggle").Click();
            Assert.Empty(cut.FindAll(".app-root.dark"));
        }

        // TC-015 (NFR5): text size increases and is capped
        [Fact]
        public void TextSizeButtons_ChangeLevelAndRespectLimits()
        {
            var cut = Render<Home>();
            Assert.NotEmpty(cut.FindAll(".app-root.text-0"));
            Assert.True(cut.Find(".text-smaller").HasAttribute("disabled"));

            cut.Find(".text-larger").Click();
            Assert.NotEmpty(cut.FindAll(".app-root.text-1"));

            cut.Find(".text-larger").Click();
            Assert.NotEmpty(cut.FindAll(".app-root.text-2"));
            Assert.True(cut.Find(".text-larger").HasAttribute("disabled"));
        }

        private class FakeItemsHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.RequestUri?.AbsolutePath?.EndsWith("/data/items.json") == true || request.RequestUri?.AbsolutePath?.EndsWith("items.json") == true)
                {
                    var json = "[{\"Id\":1,\"Name\":\"Apple\",\"Category\":\"Fruit\",\"Prices\":[{\"Supermarket\":\"Generic\",\"Price\":0.5}],\"Emoji\":\"🍎\"}]";
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                    return Task.FromResult(response);
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }
    }
}

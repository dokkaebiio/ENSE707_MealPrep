using Blazored.LocalStorage;
using Bunit;
using MealPrep.Models;
using MealPrep.Pages;
using MealPrep.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;


namespace MealPrep.Tests
{
    public class BudgetTests : HomeTestBase
    {
        //This file focuses on testing all budget related requirements (FR3, FR5, FR6) including setting budgets, adding items, and handling over-budget scenarios.
        // And accessibility features like dark mode and text size adjustments (NFR5).

        private static readonly SpendingHistoryService Service = new(null!);

        private static SpendingRecord Trip(int daysAgo, decimal total) =>
            new() { Date = new DateTime(2026, 10, 1).AddDays(-daysAgo), Total = total };


        // Set 1.00 budget, add 3 items at $0.50 each, and verify that the over-budget modal appears.
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

        // TC-005 || FR5, FR6 || positive
        [Fact]
        public async Task TC005_SetValidBudget_ShowsBudgetAndLookingGood()
        {
            // Arrange
            var cut = Render<Home>();

            await SetBudgetAsync(cut, "100");
            await AddFirstItemAsync(cut);

            Assert.Contains("$100.00", cut.Find(".budget-amount").TextContent);
            Assert.Contains("Looking good", cut.Find(".budget-status-label").TextContent);
        }

        // TC-006 || FR5, FR6 || negative, invalid budget input
        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("0")]
        [InlineData("-10")]
        public async Task TC006_InvalidBudget_ClearsBudgetAndShowsNoBudgetSet(string invalidInput)
        {
            var cut = Render<Home>();
            await SetBudgetAsync(cut, "100");
            Assert.Contains("$100.00", cut.Find(".budget-amount").TextContent);   // make sure budget is set before testing invalid input

            await SetBudgetAsync(cut, invalidInput);

            Assert.Contains("Budget: —", cut.Find(".budget-amount").TextContent);
            Assert.Contains("No budget set", cut.Find(".budget-status-label").TextContent);
        }

        // TC-007 || FR5, FR6 || negative, boundary case, adding item that exceeds budget
        [Fact]
        public async Task TC007_AddItemOverBudget_ShowsModalWithNameAndNewTotal()
        {
            var cut = await OpenOverBudgetPromptAsync();

            var modal = cut.Find(".modal-box");
            Assert.Contains("Apple", modal.TextContent);
            Assert.Contains("$1.50", modal.TextContent);
            Assert.Contains("over your $1.00 budget", modal.TextContent);
            Assert.Equal(3, cut.FindAll(".modal-actions button").Count);   // add anyway / cancel / change budget
        }

        // TC-008 || FR5, FR6 || positive, boundary case, adding item that exceeds budget and clicking "Add anyway"
        [Fact]
        public async Task TC008_ModalAddAnyway_ClosesModalAddsItemAndUpdatesTotal()
        {
            var cut = await OpenOverBudgetPromptAsync();

            await ClickAsync(cut, ".modal-actions button", 0);   // Add anyway

            Assert.Empty(cut.FindAll(".modal-box"));
            Assert.Contains("Qty: 3", cut.Find(".cart-item-qty").TextContent);
            Assert.Contains("$1.50", cut.Find(".total-line-main").TextContent);
        }

        // TC-009 | FR5, FR6 | negative
        [Fact]
        public async Task TC009_ModalCancel_ClosesModalAndLeavesCartUnchanged()
        {
            var cut = await OpenOverBudgetPromptAsync();

            await ClickAsync(cut, ".modal-actions button", 1);   // Cancel

            Assert.Empty(cut.FindAll(".modal-box"));
            Assert.Contains("Qty: 2", cut.Find(".cart-item-qty").TextContent);
            Assert.Contains("$1.00", cut.Find(".total-line-main").TextContent);
        }

        // TC-010 || FR5, FR6 || positive
        [Fact]
        public async Task TC010_ModalChangeBudget_ClosesModalLeavesCartAndBudgetEditable()
        {
            var cut = await OpenOverBudgetPromptAsync();

            await ClickAsync(cut, ".modal-actions button", 2);   // Change budget

            Assert.Empty(cut.FindAll(".modal-box"));
            Assert.Contains("Qty: 2", cut.Find(".cart-item-qty").TextContent);
            Assert.False(cut.Find("#budget").HasAttribute("disabled"));

            await SetBudgetAsync(cut, "5");                      // the user now edits the budget
            Assert.Contains("$5.00", cut.Find(".budget-amount").TextContent);
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

        //TC-23 || FR3 || positive (single trip in history)
        [Fact]
        public void TC023_SingleTrip_SuggestsThatTripsTotal()
        {
            var result = Service.GetSuggestedBudget(new List<SpendingRecord> { Trip(1, 12.34m) });

            Assert.Equal(12.34m, result);
        }

        //TC-24 || FR3 || positive (only use top 5 when multiple trips in history)
        [Fact]
        public void TC024_SixTrips_UsesOnlyTheNewestFive()
        {
            var history = new List<SpendingRecord>
            {
                Trip(6, 100m),   // oldest: must be ignored (would make the average 25 if counted)
                Trip(5, 10m), Trip(4, 10m), Trip(3, 10m), Trip(2, 10m), Trip(1, 10m)
            };

            Assert.Equal(10.00m, Service.GetSuggestedBudget(history));
        }
    }
}

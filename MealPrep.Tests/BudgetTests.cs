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


        // TC-014 (NFR5): dark mode toggle
        [Fact]
        public void DarkModeToggle_AddsAndRemovesDarkClass()
        {
            var cut = RenderInLayout(typeof(Home));
            Assert.Empty(cut.FindAll(".app-root.dark"));

            cut.Find(".theme-toggle").Click();
            Assert.NotEmpty(cut.FindAll(".app-root.dark"));

            cut.Find(".theme-toggle").Click();
            Assert.Empty(cut.FindAll(".app-root.dark"));
        }

        // TC-025 (NFR5): accessibility settings are also available on the Login and Sign Up pages
        [Theory]
        [InlineData(typeof(Login))]
        [InlineData(typeof(Signup))]
        public void TC025_AuthPages_HaveWorkingAccessibilitySettings(Type pageType)
        {
            var cut = RenderInLayout(pageType);
            Assert.NotEmpty(cut.FindAll(".auth-form"));   // the auth page itself rendered

            cut.Find(".theme-toggle").Click();
            Assert.NotEmpty(cut.FindAll(".app-root.dark"));

            cut.Find(".text-larger").Click();
            Assert.NotEmpty(cut.FindAll(".app-root.text-1"));
        }

        //TC-23 || FR3 || positive (single trip in history)
        [Fact]
        public void TC023_SingleTrip_SuggestsThatTripsTotal()
        {
            var result = Service.GetSuggestedBudget(new List<SpendingRecord> { Trip(1, 12.34m) });

            Assert.Equal(12.34m, result);
        }
    }
}

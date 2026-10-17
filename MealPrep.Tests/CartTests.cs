using Blazored.LocalStorage;
using Bunit;
using MealPrep.Pages;
using MealPrep.Services;
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

    // Focused on testing Cart Management (adding/removing items, updating quantities, and calculating total price).
    public class CartTests : HomeTestBase
    {

        private string MainTotal(IRenderedComponent<Home> cut) => cut.Find(".total-line-main").TextContent;
        private string SubTotal(IRenderedComponent<Home> cut) => cut.Find(".total-line-sub").TextContent;

        // TC-001 || FR6, FR5 || positive
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

        // TC-002 | FR5, FR6 | positive
        [Fact]
        public async Task TC002_AddSameItemTwice_ShowsQtyTwoInOneRow()
        {
            var cut = Render<Home>();

            await AddFirstItemAsync(cut);
            await AddFirstItemAsync(cut);

            Assert.Single(cut.FindAll(".cart-item-row"));
            Assert.Contains("Qty: 2", cut.Find(".cart-item-qty").TextContent);
        }

        // TC-003 | FR5, FR6 | positive
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

        // TC-004
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

        // TC-011 | FR2/FR6 search | negative
        [Fact]
        public void TC011_SearchWithNoMatch_ShowsNoItemsFoundMessage()
        {
            var cut = Render<Home>();

            cut.Find(".search-box").Input("zzzznonexistentitem");

            Assert.Contains("No items found", cut.Find(".empty-msg").TextContent);
        }

        //TC-19 || FR5 || boundary (no items in cart)
        [Fact]
        public void TC019_EmptyCart_ShowsZeroForAllThreeFigures()
        {
            var cut = Render<Home>();

            Assert.Contains("$0.00", MainTotal(cut));
            Assert.Contains("Excl. GST: $0.00", SubTotal(cut));
            Assert.Contains("GST: $0.00", SubTotal(cut));
        }

        //TC-20 || FR5 || positive
        [Fact]
        public async Task TC020_SingleItem_ShowsInclusiveExclusiveAndGst()
        {
            var cut = Render<Home>();

            await AddFirstItemAsync(cut);

            Assert.Contains("Total (incl. GST): $0.50", MainTotal(cut));
            Assert.Contains("Excl. GST: $0.43", SubTotal(cut));
            Assert.Contains("GST: $0.07", SubTotal(cut));
        }

        //TC-21 || FR5 || positive (recalculate after removing item)
        [Fact]
        public async Task TC021_RemovingAnItem_RecalculatesAllThreeFigures()
        {
            UseBackend(TestData.Items(TestData.Apple, TestData.Banana));
            var cut = Render<Home>();
            await ClickAsync(cut, "button.add-btn", 0);   // Apple  $0.50
            await ClickAsync(cut, "button.add-btn", 1);   // Banana $0.30
            Assert.Contains("Excl. GST: $0.70", SubTotal(cut));   // 0.80 / 1.15 = 0.6957

            await ClickAsync(cut, ".remove-btn", 0);      // remove Apple

            Assert.Contains("Total (incl. GST): $0.30", MainTotal(cut));
            Assert.Contains("Excl. GST: $0.26", SubTotal(cut));
            Assert.Contains("GST: $0.04", SubTotal(cut));
        }

        //TC-22 || FR6 || positive
        [Fact]
        public async Task TC022_TypedQuantity_AddsThatManyAndTotalMatches()
        {
            var cut = Render<Home>();

            cut.Find(".qty-input").Change("3");
            await AddFirstItemAsync(cut);

            Assert.Contains("Qty: 3", cut.Find(".cart-item-qty").TextContent);
            Assert.Contains("Total (incl. GST): $1.50", cut.Find(".total-line-main").TextContent);
        }
    }
}

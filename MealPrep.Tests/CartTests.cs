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

        //TC-19 || FR5 || boundary (no items in cart)
        [Fact]
        public void TC019_EmptyCart_ShowsZeroForAllThreeFigures()
        {
            var cut = Render<Home>();

            Assert.Contains("$0.00", MainTotal(cut));
            Assert.Contains("Excl. GST: $0.00", SubTotal(cut));
            Assert.Contains("GST: $0.00", SubTotal(cut));
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

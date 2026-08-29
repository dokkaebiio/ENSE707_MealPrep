using Bunit;
using MealPrep.Pages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MealPrep.Tests
{
    public class CartTests : BunitContext
    {
        public CartTests()
        {
            // register a fake HttpClient that returns a single GroceryItem for /data/items.json
            var handler = new FakeItemsHandler();
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            Services.AddSingleton<HttpClient>(client);
        }

        [Fact]
        public void ClickingAddButton_AddsItemToCartUi()
        {
            // Arrange
            var cut = Render<Home>();

            // Act - click the Add button for the single item
            cut.Find("button.add-btn").Click();

            // Assert - cart shows one row
            var cartItems = cut.FindAll(".cart-item-row");
            Assert.Single(cartItems);

            // Assert - item name and qty present
            var name = cut.Find(".cart-item-info .item-name").TextContent.Trim();
            Assert.Contains("Apple", name);

            var qty = cut.Find(".cart-item-qty").TextContent.Trim();
            Assert.Contains("Qty: 1", qty);

            // Assert - unit price shown in cart
            var priceText = cut.Find(".cart-item-info .item-price").TextContent.Trim();
            Assert.Contains("$0.50", priceText);

            // Assert - remove button exists
            var remove = cut.Find(".remove-btn");
            Assert.NotNull(remove);
        }

        private class FakeItemsHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.RequestUri?.AbsolutePath?.EndsWith("/data/items.json") == true || request.RequestUri?.AbsolutePath?.EndsWith("items.json") == true)
                {
                    var json = "[{\"Id\":1,\"Name\":\"Apple\",\"Category\":\"Fruit\",\"Price\":0.5,\"Emoji\":\"🍎\"}]";
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

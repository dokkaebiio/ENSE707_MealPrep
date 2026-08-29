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

        // TC-001
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

        // TC-002
        [Fact]
        public async Task AddSameItemTwice_IncrementsQtyInCartUi()
        {
            // Arrange
            var cut = Render<Home>();
            // Act - click the Add button for the single item twice
            await cut.InvokeAsync(() => cut.Find("button.add-btn").Click());
            await cut.InvokeAsync(() => cut.Find("button.add-btn").Click());
            // Assert - cart shows one row
            var cartItems = cut.FindAll(".cart-item-row");
            Assert.Single(cartItems);
            // Assert - item name and qty present
            var name = cut.Find(".cart-item-info .item-name").TextContent.Trim();
            Assert.Contains("Apple", name);
            var qty = cut.Find(".cart-item-qty").TextContent.Trim();
            Assert.Contains("Qty: 2", qty);
        }

        // TC-003
        [Fact]
        public async Task AddDifferentItems_AddsBothItemsToCartUi()
        {
            // Arrange
            // Use a different fake handler that returns two items
            var handler = new FakeItemsHandlerWithTwoItems();
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            Services.AddSingleton<HttpClient>(client);
            var cut = Render<Home>();
            // Act - click the Add button for both items
            await cut.InvokeAsync(() => cut.FindAll("button.add-btn")[0].Click()); // Add Apple
            await cut.InvokeAsync(() => cut.FindAll("button.add-btn")[1].Click()); // Add Banana
            // Assert - cart shows two rows
            var cartItems = cut.FindAll(".cart-item-row");
            Assert.Equal(2, cartItems.Count);
            //Assert - total price is correct
            var totalPriceText = cut.Find(".cart-summary-total").TextContent.Trim();
            Assert.Contains("$0.80", totalPriceText); // 0.5 + 0.3 = 0.8
        }

        // TC-004
        [Fact]
        public async Task RemoveItemFromCart_RemovesItemUi()
        {
            // Arrange
            var cut = Render<Home>();
            // Act - click the Add button for the single item
            await cut.InvokeAsync(() => cut.Find("button.add-btn").Click());
            // Assert - cart shows one row
            var cartItems = cut.FindAll(".cart-item-row");
            Assert.Single(cartItems);
            // Act - click the Remove button
            await cut.InvokeAsync(() => cut.Find(".remove-btn").Click());
            // Assert - cart shows no rows
            cartItems = cut.FindAll(".cart-item-row");
            Assert.Empty(cartItems);
            // Assert - total price is $0.00
            var totalPriceText = cut.Find(".cart-summary-total").TextContent.Trim();
            Assert.Contains("$0.00", totalPriceText);
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

        private class FakeItemsHandlerWithTwoItems : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.RequestUri?.AbsolutePath?.EndsWith("/data/items.json") == true || request.RequestUri?.AbsolutePath?.EndsWith("items.json") == true)
                {
                    var json = "[{\"Id\":1,\"Name\":\"Apple\",\"Category\":\"Fruit\",\"Price\":0.5,\"Emoji\":\"🍎\"}," +
                               "{\"Id\":2,\"Name\":\"Banana\",\"Category\":\"Fruit\",\"Price\":0.3,\"Emoji\":\"🍌\"}]";
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

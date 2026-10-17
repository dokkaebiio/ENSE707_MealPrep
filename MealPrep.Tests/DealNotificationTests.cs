using System.Text.Json;
using Bunit;
using MealPrep.Models;
using MealPrep.Pages;
using MealPrep.Services;
using Xunit;

namespace MealPrep.Tests
{
    // FR4: notify the user of updated discount deals.
    // TC-024 to TC-027 test DealNotifier directly. TC-028 to TC-031 go through the page.
    public class DealNotificationTests : HomeTestBase
    {
        private static readonly DateTime Now = new(2026, 10, 17, 12, 0, 0);

        private static Deal MakeDeal(int id, DateTime updatedAt, DateTime expiresOn) => new()
        {
            Id = id,
            ItemId = 1,
            Supermarket = "Generic",
            DealPrice = 0.40m,
            UpdatedAt = updatedAt,
            ExpiresOn = expiresOn
        };

        private static string OneActiveDealJson() =>
            JsonSerializer.Serialize(new[] { MakeDeal(1, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(7)) });

        // TC-016 | FR4 | boundary (expiry)
        [Fact]
        public void TC016_ExpiredDealsAreExcluded_DealExpiringExactlyNowIsKept()
        {
            var deals = new[]
            {
                MakeDeal(1, Now.AddDays(-2), Now.AddSeconds(-1)),   // expired one second ago
                MakeDeal(2, Now.AddDays(-2), Now),                  // expires exactly now
                MakeDeal(3, Now.AddDays(-2), Now.AddDays(3))        // still running
            };

            var active = DealNotifier.GetActiveDeals(deals, Now);

            Assert.Equal(new[] { 2, 3 }, active.Select(d => d.Id).OrderBy(id => id));
        }

        // TC-017 | FR4 | positive (newest update first)
        [Fact]
        public void TC017_ActiveDeals_AreListedNewestUpdateFirst()
        {
            var deals = new[]
            {
                MakeDeal(1, Now.AddDays(-5), Now.AddDays(10)),
                MakeDeal(2, Now.AddDays(-1), Now.AddDays(10)),
                MakeDeal(3, Now.AddDays(-3), Now.AddDays(10))
            };

            var active = DealNotifier.GetActiveDeals(deals, Now);

            Assert.Equal(new[] { 2, 3, 1 }, active.Select(d => d.Id));
        }

        // TC-018 | FR4 | error handling (deals.json missing)
        [Fact]
        public async Task TC018_MissingDealsFile_PageStillWorksAndShowsNoDeals()
        {
            // UseBackend with no deals file: the fake server answers 404 for deals.json.
            UseBackend(TestData.Items(TestData.Apple), dealsJson: null);

            var cut = Render<Home>();

            Assert.NotEmpty(cut.FindAll(".item-card"));        // the rest of the page loaded
            Assert.Empty(cut.FindAll(".notif-count"));

            await ClickAsync(cut, ".notif-bell");
            Assert.Contains("No deals right now", cut.Find(".notif-panel").TextContent);
        }
    }
}
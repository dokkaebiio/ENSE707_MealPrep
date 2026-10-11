using MealPrep.Models;

namespace MealPrep.Services
{
    public static class DealNotifier
    {
        // Deals that haven't expired yet, newest update first.
        public static List<Deal> GetActiveDeals(IEnumerable<Deal> deals, DateTime now) =>
            deals.Where(d => d.ExpiresOn >= now)
                 .OrderByDescending(d => d.UpdatedAt)
                 .ToList();

        // Active deals updated since the user last marked notifications as read.
        // lastSeen == null means they've never opened them, so every active deal is new.
        public static List<Deal> GetUnseenDeals(IEnumerable<Deal> deals, DateTime? lastSeen, DateTime now) =>
            GetActiveDeals(deals, now)
                .Where(d => lastSeen is null || d.UpdatedAt > lastSeen)
                .ToList();
    }
}
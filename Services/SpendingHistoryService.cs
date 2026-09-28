using Blazored.LocalStorage;
using MealPrep.Models;

namespace MealPrep.Services
{
    public class SpendingHistoryService
    {
        private readonly ILocalStorageService _localStorage;

        public SpendingHistoryService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        private static string KeyFor(string email) => $"mealprep_spending_{email.ToLowerInvariant()}";

        public async Task<List<SpendingRecord>> GetHistoryAsync(string email)
        {
            return await _localStorage.GetItemAsync<List<SpendingRecord>>(KeyFor(email)) ?? new();
        }

        public async Task LogTripAsync(string email, decimal total)
        {
            var history = await GetHistoryAsync(email);
            history.Add(new SpendingRecord { Date = DateTime.Now, Total = total });
            await _localStorage.SetItemAsync(KeyFor(email), history);
        }

        // FR3: suggest a budget based on the user's past spending.
        // Average of the most recent N trips; null if there's no history yet.
        public decimal? GetSuggestedBudget(List<SpendingRecord> history, int lastN = 5)
        {
            if (!history.Any())
                return null;

            var recent = history.OrderByDescending(r => r.Date).Take(lastN);
            return Math.Round(recent.Average(r => r.Total), 2);
        }
    }
}
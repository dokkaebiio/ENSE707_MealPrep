using Blazored.LocalStorage;

namespace MealPrep.Services
{
    // Accessibility settings (NFR5), shared by every page through MainLayout.
    // Global keys rather than per-user, so the theme doesn't flip on login/logout.
    public class AccessibilitySettings
    {
        private readonly ILocalStorageService _localStorage;

        private const string DarkKey = "mealprep_dark";
        private const string TextLevelKey = "mealprep_textlevel";

        public const int MinTextLevel = 0;
        public const int MaxTextLevel = 2;

        public bool IsDark { get; private set; }
        public int TextLevel { get; private set; }   // 0 = normal, 1 = large, 2 = extra large

        public AccessibilitySettings(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        // Called once at app startup (see Program.cs) so the first render already uses the saved settings.
        public async Task InitializeAsync()
        {
            IsDark = await _localStorage.GetItemAsync<bool>(DarkKey);
            TextLevel = Math.Clamp(await _localStorage.GetItemAsync<int>(TextLevelKey), MinTextLevel, MaxTextLevel);
        }

        public async Task ToggleThemeAsync()
        {
            IsDark = !IsDark;
            await _localStorage.SetItemAsync(DarkKey, IsDark);
        }

        public async Task ChangeTextSizeAsync(int delta)
        {
            TextLevel = Math.Clamp(TextLevel + delta, MinTextLevel, MaxTextLevel);
            await _localStorage.SetItemAsync(TextLevelKey, TextLevel);
        }
    }
}

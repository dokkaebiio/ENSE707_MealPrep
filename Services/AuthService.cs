// Services/AuthService.cs
using Blazored.LocalStorage;
using MealPrep.Models;

namespace MealPrep.Services
{
    public class AuthService
    {
        private readonly ILocalStorageService _localStorage;
        private List<AppUser> _users = new();

        private const string UsersKey = "mealprep_users";
        private const string SessionKey = "mealprep_current_user_email";

        public AppUser? CurrentUser { get; private set; }

        public AuthService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        // Loads persisted users and restores the logged-in session, if any.
        // Called once at app startup (see Program.cs) before any page renders.
        public async Task InitializeAsync()
        {
            _users = await _localStorage.GetItemAsync<List<AppUser>>(UsersKey) ?? new();

            var savedEmail = await _localStorage.GetItemAsync<string>(SessionKey);
            if (!string.IsNullOrEmpty(savedEmail))
            {
                CurrentUser = _users.FirstOrDefault(u =>
                    u.Email.Equals(savedEmail, StringComparison.OrdinalIgnoreCase));
            }
        }

        public async Task<bool> SignUpAsync(string name, string email, string password, ErrorBox error)
        {
            if (_users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                error.Message = "An account with this email already exists.";
                return false;
            }

            _users.Add(new AppUser { Name = name, Email = email, Password = password });
            await _localStorage.SetItemAsync(UsersKey, _users);
            return true;
        }

        public async Task<bool> LoginAsync(string email, string password, ErrorBox error)
        {
            var user = _users.FirstOrDefault(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.Password == password);

            if (user is null)
            {
                error.Message = "Invalid email or password.";
                return false;
            }

            CurrentUser = user;
            await _localStorage.SetItemAsync(SessionKey, user.Email);
            return true;
        }

        public async Task LogoutAsync()
        {
            CurrentUser = null;
            await _localStorage.RemoveItemAsync(SessionKey);
        }
    }

    // C# doesn't allow "out" parameters on async methods, so this small
    // wrapper class carries the error message back instead.
    public class ErrorBox
    {
        public string Message { get; set; } = "";
    }
}
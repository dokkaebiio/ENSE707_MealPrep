
namespace MealPrep.Models
{
    // Prototype-only: plain-text password, no hashing/encryption.
    // Real authentication security is out of scope for this phase.
    public class AppUser
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
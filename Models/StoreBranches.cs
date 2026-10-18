using System.Linq;
using System.Collections.Generic;

namespace MealPrep.Models
{
    // Small, illustrative set of branches for one city (Auckland), per
    // prototype scope — not a complete national store network.
    public static class StoreBranches
    {
        public static readonly List<Branch> All = new()
        {
            new Branch { Name = "PAK'nSAVE Royal Oak", Supermarket = "PAK'nSAVE", Suburb = "Royal Oak", City = "Auckland", PriceModifier = 1.00m },
            new Branch { Name = "PAK'nSAVE Westgate", Supermarket = "PAK'nSAVE", Suburb = "Westgate", City = "Auckland", PriceModifier = 1.03m },
            new Branch { Name = "New World Three Kings", Supermarket = "New World", Suburb = "Three Kings", City = "Auckland", PriceModifier = 1.00m },
            new Branch { Name = "New World Remuera", Supermarket = "New World", Suburb = "Remuera", City = "Auckland", PriceModifier = 1.05m },
            new Branch { Name = "Woolworths Ponsonby", Supermarket = "Woolworths", Suburb = "Ponsonby", City = "Auckland", PriceModifier = 1.00m },
            new Branch { Name = "Woolworths Mount Eden", Supermarket = "Woolworths", Suburb = "Mount Eden", City = "Auckland", PriceModifier = 0.97m },
        };

        public static List<string> Cities => All.Select(b => b.City).Distinct().OrderBy(c => c).ToList();
    }
}

namespace MealPrep.Models
{
    // One row from the preset item dataset (wwwroot/data/items.json).
    public class GroceryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Emoji { get; set; } = "";

        // One or more supermarket price points (FR2). Items without real
        // comparison data have a single "Generic" entry as a placeholder.
        public List<SupermarketPrice> Prices { get; set; } = new();

        // Cheapest available price — keeps existing cart/budget code
        // (which reads item.Price) working unchanged.
        public decimal Price => Prices.Any() ? Prices.Min(p => p.Price) : 0m;

        public string? CheapestSupermarket =>
            Prices.OrderBy(p => p.Price).FirstOrDefault()?.Supermarket;

        // True only for items with real multi-supermarket data, not just
        // a single generic placeholder price.
        public bool HasPriceComparison => Prices.Count > 1;
    }
}
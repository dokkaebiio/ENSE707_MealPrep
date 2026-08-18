namespace GroceryBudgetWeb.Models
{
    // One row from the preset item dataset (wwwroot/data/items.json).
    // Kept as a separate JSON file, not hard-coded, so items/prices can be
    // updated without changing code (NFR5 - maintainability).
    public class GroceryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Price { get; set; }
        public string Emoji { get; set; } = "";
    }
}

namespace MealPrep.Models
{
    // One row in the user's current grocery list.
    // Blazor re-renders the component on any state change, so this doesn't
    // need INotifyPropertyChanged the way the WPF version's models did.
    public class ListLineItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }

        public decimal LineTotal => UnitPrice * Qty;

        // Copy of the item's full per-supermarket prices, captured when added
        // to the cart, so whole-cart totals can be computed per supermarket (FR7).
        public List<SupermarketPrice> Prices { get; set; } = new();
    }
}

namespace MealPrep.Models
{
    // One discount deal on one item at one supermarket brand.
    // UpdatedAt is what drives notifications: a deal is "new" if it was
    // updated after the user last marked their notifications as read.
    public class Deal
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string Supermarket { get; set; } = "";
        public decimal DealPrice { get; set; }
        public DateTime ExpiresOn { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
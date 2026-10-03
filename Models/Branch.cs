namespace MealPrep.Models
{
    // A specific physical store belonging to a supermarket brand.
    // PriceModifier is a small synthetic adjustment applied to the brand-level
    // price, illustrating that individual branches price slightly differently
    // (a real, documented NZ behaviour — PAK'nSAVE specifically prices per
    // store). This is NOT real per-branch pricing data, since none is
    // publicly available — see Task 4 for the AI-data-fabrication discussion
    // this feature is built around.
    public class Branch
    {
        public string Name { get; set; } = "";
        public string Supermarket { get; set; } = ""; // brand, e.g. "PAK'nSAVE"
        public string Suburb { get; set; } = "";
        public string City { get; set; } = "";
        public decimal PriceModifier { get; set; } = 1.0m;
    }
}

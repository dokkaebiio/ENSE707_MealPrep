# Roadmap: Build the Cart + Budget Planner Yourself

This project is intentionally bare — `Pages/Home.razor` just proves everything runs.
Everything below, you build. Do the steps in order; each one only needs what came before it.

Run `dotnet run` after every step and check it in the browser before moving on —
small verified steps beat writing everything then debugging it all at once.

> If your machine only has .NET 6 installed (not .NET 8), open `GroceryCartApp.csproj`
> and change `net8.0` to `net6.0`, and both package versions from `8.0.8` to `6.0.0`.

---

## Step 1 — Design your data

Decide what a grocery item needs: probably an id, a name, a category, and a price.

Create `Models/GroceryItem.cs`:
```csharp
namespace GroceryCartApp.Models
{
    public class GroceryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Price { get; set; }
    }
}
```

Then create a small dataset by hand — `wwwroot/data/items.json` — with 5-10 items to start:
```json
[
  { "id": 1, "name": "Milk (2L)", "category": "Dairy", "price": 4.30 },
  { "id": 2, "name": "Bread", "category": "Bakery", "price": 3.20 }
]
```

**Why a separate JSON file instead of hard-coding the list in C#?** This is literally your NFR5-equivalent (maintainability) — keeping data separate from logic. Add `@using GroceryCartApp.Models` to `_Imports.razor` once this class exists.

---

## Step 2 — Load the data into the page

In `Home.razor`, inject `HttpClient` and fetch the JSON when the page starts.

Concepts you need:
- `@inject HttpClient Http` at the top of the file — makes `Http` available in your code
- `OnInitializedAsync()` — a lifecycle method Blazor calls once when the component loads
- `Http.GetFromJsonAsync<List<GroceryItem>>("path")` — fetches and parses JSON in one call

Try writing this yourself: declare a `private List<GroceryItem> AllItems = new();` field, and inside `OnInitializedAsync`, assign it from the fetch call. Print `AllItems.Count` somewhere on the page to prove it loaded.

---

## Step 3 — Display the items

Use `@foreach` inside your markup to loop over `AllItems` and render each one — name, category, price. Doesn't need to look good yet, just prove the data renders.

```razor
@foreach (var item in AllItems)
{
    <div>@item.Name - $@item.Price</div>
}
```

---

## Step 4 — Build the cart

This is the core "add to cart" mechanic.

1. Create `Models/CartLineItem.cs` — similar to `GroceryItem`, but with a `Qty` field and a computed `LineTotal` (`UnitPrice * Qty`).
2. Add `private List<CartLineItem> Cart = new();` to `Home.razor`.
3. Add an "Add" button next to each item in your loop. Wire its `@onclick` to a method that:
   - checks if that item is already in `Cart` (use `.FirstOrDefault(...)`)
   - if yes, increase its `Qty`
   - if no, add a new `CartLineItem`

Key concept: **Blazor re-renders automatically whenever your `@code` fields change** after an event handler runs — you don't need to manually "refresh" the page.

---

## Step 5 — Show the cart and let people manage it

Loop over `Cart` similarly to Step 3, but also add:
- A way to change quantity (an `<input type="number">` with an `@onchange` handler)
- A remove button (`Cart.Remove(item)` in its `@onclick`)

This is where FR6 ("view, create and manage shopping list") actually gets satisfied.

---

## Step 6 — Add the budget planner

1. Add `private decimal Budget = 100m;` and bind an `<input type="number">` to it with `@bind="Budget"`.
2. Add a computed property:
   ```csharp
   private decimal RunningTotal => Cart.Sum(c => c.LineTotal);
   ```
   (This is FR5 — estimated total cost. GST can come later: multiply by 1.15 or whatever your local rate is, once the base total works.)
3. Compare `Budget` to `RunningTotal` and show whether they're over or under — an `if`/`else if`/`else` in your markup, similar to Step 3's loop.

---

## Step 7 — Polish (only once 1-6 work)

- Search/filter box over the item list
- Category dropdown
- Styling in `wwwroot/css/app.css`
- A collapsible sidebar for the cart (ask me for the concept walkthrough again once you're here — it's a CSS transform + a boolean toggle, nothing more)

---

## When you get stuck

Paste me the specific file and the error or unexpected behaviour, rather than asking me to write the step for you — that way you keep the understanding. I'm happy to review what you've written and point at what's wrong, or explain a concept again a different way if something isn't clicking.

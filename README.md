# OrderFlow

OrderFlow is a small internal order-management tool for a fictional retail
business, built with ASP.NET Core MVC. It tracks customers, products, and
orders, and gives an operations team a dashboard of revenue and order volume.

This repo is a **portfolio piece demonstrating a realistic bug-fix workflow**:
the app was built in two stages, with a genuine null-reference bug shipped
first and then properly diagnosed and fixed, each as its own tagged commit.

**Live demo:** http://order-flow.runasp.net

- [`before-fix`](../../tree/before-fix) — the app as originally shipped, bug included
- [`after-fix`](../../tree/after-fix) — the same app with the bug fixed and a regression test added

Jump to the [Bug Fix Case Study](#bug-fix-case-study) below for the full
diagnosis and fix.

## What it does

- **Dashboard** — total revenue, order count, average order value, and
  pending-order count, plus a table of recent orders.
- **Order list** — every order, filterable by status (Pending / Processing /
  Shipped / Completed / Cancelled).
- **Order detail** — line items, quantities, unit prices, and the computed
  total for a single order.
- **Create / edit order** — pick a customer, a status, an order date, and up
  to several product/quantity line items.

## Tech stack

- ASP.NET Core MVC on **.NET 8**
- **Entity Framework Core** with **SQLite** (a single portable `.db` file —
  no external database server needed to run it locally or on shared hosting)
- **xUnit** for unit tests
- Server-rendered Razor views, vanilla CSS (no CSS framework), Google Fonts
  (Fraunces for display type, Inter for body text)

### Design direction

The UI intentionally avoids the generic Bootstrap-admin-template look: deep
teal (`#0F4C4C`) as the primary color, warm coral (`#E76F51`) as a single
accent reserved for calls-to-action, an off-white background (`#FAF7F2`), a
serif display font for headings, and a hand-built top nav rather than the
default Bootstrap navbar.

## Running it locally

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
git clone https://github.com/<your-username>/orderflow-bugfix-demo.git
cd orderflow-bugfix-demo/src/OrderFlow
dotnet run
```

The app listens on the URL printed in the console (typically
`http://localhost:5000` or similar). On first run it automatically:

1. Applies EF Core migrations to create `orderflow.db` (a SQLite file in the
   project directory — nothing to install or configure).
2. Seeds sample customers, products, and orders, including one order that
   deliberately reproduces the historical data-import edge case described
   below.

To reset the data, stop the app and delete `orderflow.db*` before running
again.

### Running the tests

```bash
cd orderflow-bugfix-demo
dotnet test
```

## Project structure

```
orderflow-bugfix-demo/
├── src/OrderFlow/          # the ASP.NET Core MVC app
│   ├── Controllers/
│   ├── Models/              # Order, OrderItem, Customer, Product
│   ├── Data/                # DbContext, EF Core migrations, seed data
│   ├── ViewModels/
│   └── Views/
└── tests/OrderFlow.Tests/   # xUnit tests
```

## Bug Fix Case Study

### The setup

Every order line item (`OrderItem`) links to a catalog `Product` so the UI
can show its name and price. That link is nullable by design: a `Product`
can be discontinued or re-SKU'd, and old orders that referenced it need to
keep working for historical reporting. The seed data includes one order
imported from a legacy POS system where a line item's SKU (`LEG-4471`,
"Bamboo Cutting Board") no longer matches anything in the catalog — a
completely ordinary consequence of importing historical data after a product
was discontinued.

### The bug (`before-fix`)

The order total was computed straight from the live `Product` navigation
property:

```csharp
// OrderItem.cs
[NotMapped]
public decimal LineTotal => Product!.Price * Quantity;

// Order.cs
[NotMapped]
public decimal TotalAmount => OrderItems.Sum(item => item.LineTotal);
```

The `!` (null-forgiving operator) was silencing the compiler's nullable
warning on the assumption that "every order is created against a catalog
product, so `Product` is always populated." That's true for orders created
through the UI — but not for the legacy-imported line item, whose `Product`
is `null`.

### The error it caused

Any page that touched an order total — the dashboard's revenue figure, the
order list, and that order's own detail page — crashed with:

```
System.NullReferenceException: Object reference not set to an instance of an object.
   at OrderFlow.Models.OrderItem.get_LineTotal() in OrderItem.cs:line 27
   at OrderFlow.Models.Order.<>c.<get_TotalAmount>b__29_0(OrderItem item) in Order.cs:line 23
   at System.Linq.Enumerable.Sum[TSource,TResult,TAccumulator](IEnumerable`1 source, Func`2 selector)
   at OrderFlow.Models.Order.get_TotalAmount() in Order.cs:line 23
   at OrderFlow.Controllers.DashboardController.Index() in DashboardController.cs:line 29
```

One bad row — a completely realistic side effect of a historical data
import — took down the dashboard and the order list for every order, not
just the affected one.

### The fix (`after-fix`)

The real problem wasn't the missing null check by itself; it was that the
code conflated two different questions: *"what did this line item cost?"*
and *"can we still resolve it to a live catalog product?"* Those need to be
answered independently.

```csharp
// OrderItem.cs
// Snapshot of the unit price at the moment this line was created. This is
// the single source of truth for totals: captured once, up front, so it
// survives catalog price changes and never depends on the Product
// navigation property being resolvable.
[Range(0, 100000)]
public decimal UnitPrice { get; set; }

[NotMapped]
public decimal LineTotal => Quantity * UnitPrice;
```

`UnitPrice` is a plain, non-nullable `decimal` stored directly on
`OrderItem`, populated once when the line item is created
(`OrdersController.BuildOrderItems`) from the resolved product's current
price. `Order` and `Product` don't need to change — `TotalAmount` still just
sums `LineTotal` across items — but that sum can no longer throw, because it
no longer depends on a navigation property that might not be loaded or might
legitimately be `null`.

Two supporting changes went with it:

- **Validation on input:** if a submitted order line references a product ID
  that can't be found (which should be unreachable through the UI's own
  dropdown, but is cheap insurance against stale form data), the controller
  now rejects the request with a clear error instead of silently building a
  priceless order item.
- **Historical backfill:** the seeded legacy-import line item now carries the
  price that was actually present in the original import file (`$24.00`),
  reflecting that the price was always available — the bug was that nothing
  captured it independently of the missing product link.

### Why the fix works

- `LineTotal` reads two plain value-type fields (`Quantity`, `UnitPrice`)
  that live on `OrderItem` itself — there's no navigation property to
  dereference, so there's nothing left that can be `null` at that call site.
- Prices become historically accurate: an order's total no longer silently
  drifts if a product's catalog price changes after the order was placed,
  which is arguably the correct behavior for an order system regardless of
  this bug.
- The one place that legitimately needs the *current* catalog price — the
  order create/edit form, when it snapshots `UnitPrice` — validates that the
  product actually exists before touching its price.

### The regression test

```csharp
[Fact]
public void TotalAmount_SumsAllLines_WhenOneItemHasNoLinkedProduct()
{
    var order = new Order
    {
        OrderItems = new List<OrderItem>
        {
            new() { ProductId = 1, Product = new Product { /* ... */ Price = 129.00m },
                    UnitPrice = 129.00m, Quantity = 1 },
            new() { ProductId = null, Product = null,
                    ProductNameSnapshot = "Bamboo Cutting Board (legacy SKU LEG-4471, discontinued)",
                    UnitPrice = 24.00m, Quantity = 2 },
        },
    };

    Assert.Equal(177.00m, order.TotalAmount);
}
```

Run against the `before-fix` code, this test throws the same
`NullReferenceException` shown above instead of failing an assertion — a
concrete demonstration that a two-minute unit test would have caught this
before it ever reached the seed data, let alone production.

## Live demo

Deployed to MonsterASP.NET: http://order-flow.runasp.net

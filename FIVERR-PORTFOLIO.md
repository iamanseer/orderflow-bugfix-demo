## Case Study: Fixing a NullReferenceException in an Order Management Dashboard

**The problem:** I built OrderFlow, an ASP.NET Core order-management tool for
a retail business, with a dashboard that shows total revenue and an order
list with per-order totals. Shortly after seeding it with realistic data —
including orders imported from a legacy system — the dashboard and order
list started throwing `NullReferenceException` on every request. The whole
admin section was down.

**My diagnosis process:** The stack trace pointed straight at the total
calculation: `OrderItem.LineTotal`, which multiplied quantity by
`Product.Price`, read from each line item's linked catalog product. I traced
it to one specific order, imported from a legacy POS system, where a line
item's SKU no longer matched any product in the catalog — the product had
since been discontinued. The code assumed every order line would always have
a resolvable product and used the null-forgiving operator (`!`) to suppress
the compiler's warning about that assumption. One historical row broke every
page that touched order totals, not just its own.

**The fix:** Rather than just adding a null check, I fixed the actual design
flaw: the code was conflating "what did this line cost" with "can we still
find its product in the catalog." I added a `UnitPrice` field directly on
`OrderItem`, captured once when the order is created, so totals never depend
on a navigation property that might be missing or stale. I added input
validation so a malformed submission gets rejected with a clear error
instead of silently creating an unpriced line item, and wrote a unit test
that recreates the exact scenario — an order with a null-linked product — to
make sure this class of bug can't ship again.

**Tech stack:** ASP.NET Core MVC (.NET 8), Entity Framework Core, SQLite,
xUnit, hand-built CSS (no framework).

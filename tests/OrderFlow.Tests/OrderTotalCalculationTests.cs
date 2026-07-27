using OrderFlow.Models;
using Xunit;

namespace OrderFlow.Tests;

// Regression coverage for the order-total bug fixed in commit "after-fix":
// OrderItem.LineTotal used to read Quantity * Product!.Price, which threw a
// NullReferenceException whenever a line item's Product link couldn't be
// resolved (e.g. a discontinued SKU from a legacy data import). Had this test
// existed before the fix, it would have failed with that exception instead of
// the bug shipping to production.
public class OrderTotalCalculationTests
{
    [Fact]
    public void LineTotal_UsesSnapshottedUnitPrice_EvenWhenProductIsNull()
    {
        var item = new OrderItem
        {
            ProductId = null,
            Product = null,
            ProductNameSnapshot = "Bamboo Cutting Board (legacy SKU LEG-4471, discontinued)",
            UnitPrice = 24.00m,
            Quantity = 2,
        };

        var lineTotal = item.LineTotal;

        Assert.Equal(48.00m, lineTotal);
    }

    [Fact]
    public void TotalAmount_SumsAllLines_WhenOneItemHasNoLinkedProduct()
    {
        var order = new Order
        {
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    ProductId = 1,
                    Product = new Product { Id = 1, Name = "Merino Wool Throw", Sku = "SKU-1003", Price = 129.00m },
                    ProductNameSnapshot = "Merino Wool Throw",
                    UnitPrice = 129.00m,
                    Quantity = 1,
                },
                new()
                {
                    ProductId = null,
                    Product = null,
                    ProductNameSnapshot = "Bamboo Cutting Board (legacy SKU LEG-4471, discontinued)",
                    UnitPrice = 24.00m,
                    Quantity = 2,
                },
            },
        };

        var total = order.TotalAmount;

        Assert.Equal(177.00m, total);
    }

    [Fact]
    public void TotalAmount_IsZero_ForOrderWithNoItems()
    {
        var order = new Order { OrderItems = new List<OrderItem>() };

        Assert.Equal(0m, order.TotalAmount);
    }
}

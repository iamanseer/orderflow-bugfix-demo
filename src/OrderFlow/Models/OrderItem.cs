using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderFlow.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    // Nullable: not every imported order line resolves to a catalog product
    // (see DbInitializer's "legacy import" seed order, where a discontinued
    // SKU no longer matches anything). Product is for catalog linkage and
    // display only -- it must never be relied on for money math, since it can
    // legitimately be null, or its Price can drift after the order was placed.
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, StringLength(150)]
    public string ProductNameSnapshot { get; set; } = string.Empty;

    [Range(1, 10000)]
    public int Quantity { get; set; }

    // Snapshot of the unit price at the moment this line was created. This is
    // the single source of truth for totals: captured once, up front, so it
    // survives catalog price changes and never depends on the Product
    // navigation property being resolvable.
    [Range(0, 100000)]
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal LineTotal => Quantity * UnitPrice;
}

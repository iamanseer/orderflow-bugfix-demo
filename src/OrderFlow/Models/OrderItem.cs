using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderFlow.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    // Nullable because not every imported order line resolves to a catalog
    // product (see DbInitializer's "legacy import" seed order).
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    [Required, StringLength(150)]
    public string ProductNameSnapshot { get; set; } = string.Empty;

    [Range(1, 10000)]
    public int Quantity { get; set; }

    // Every order is created against a catalog product, so this assumes
    // Product is always populated.
    [NotMapped]
    public decimal LineTotal => Product!.Price * Quantity;
}

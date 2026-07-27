using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderFlow.Models;

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [StringLength(500)]
    public string? Notes { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new();

    [NotMapped]
    public decimal TotalAmount => OrderItems.Sum(item => item.LineTotal);
}

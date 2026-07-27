using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Models;

public class Product
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Sku { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public List<OrderItem> OrderItems { get; set; } = new();
}

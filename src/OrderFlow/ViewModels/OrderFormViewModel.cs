using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrderFlow.Models;

namespace OrderFlow.ViewModels;

public class OrderFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please choose a customer.")]
    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Required]
    [Display(Name = "Order date")]
    [DataType(DataType.Date)]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow.Date;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [StringLength(500)]
    public string? Notes { get; set; }

    public List<OrderItemInputModel> Items { get; set; } = new();

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> ProductOptions { get; set; } = new();
}

public class OrderItemInputModel
{
    public int? ProductId { get; set; }

    [Range(0, 10000, ErrorMessage = "Quantity must be zero or a positive number.")]
    public int Quantity { get; set; }
}

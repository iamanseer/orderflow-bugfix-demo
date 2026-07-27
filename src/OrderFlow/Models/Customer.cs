using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Models;

public class Customer
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }

    public List<Order> Orders { get; set; } = new();
}

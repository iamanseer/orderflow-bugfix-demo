using OrderFlow.Models;

namespace OrderFlow.ViewModels;

public class DashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int PendingOrderCount { get; set; }
    public List<Order> RecentOrders { get; set; } = new();
}

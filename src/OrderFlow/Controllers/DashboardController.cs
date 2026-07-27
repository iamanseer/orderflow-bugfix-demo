using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Data;
using OrderFlow.Models;
using OrderFlow.ViewModels;

namespace OrderFlow.Controllers;

public class DashboardController : Controller
{
    private readonly OrderFlowContext _context;

    public DashboardController(OrderFlowContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var revenueOrders = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();

        var viewModel = new DashboardViewModel
        {
            TotalRevenue = revenueOrders.Sum(o => o.TotalAmount),
            OrderCount = orders.Count,
            AverageOrderValue = revenueOrders.Count == 0 ? 0 : revenueOrders.Sum(o => o.TotalAmount) / revenueOrders.Count,
            PendingOrderCount = orders.Count(o => o.Status == OrderStatus.Pending),
            RecentOrders = orders.Take(6).ToList(),
        };

        return View(viewModel);
    }
}

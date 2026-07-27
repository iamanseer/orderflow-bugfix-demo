using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Data;
using OrderFlow.Models;
using OrderFlow.ViewModels;

namespace OrderFlow.Controllers;

public class OrderItemValidationException : Exception
{
    public OrderItemValidationException(string message) : base(message)
    {
    }
}

public class OrdersController : Controller
{
    private const int EmptyItemRowCount = 4;

    private readonly OrderFlowContext _context;

    public OrdersController(OrderFlowContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(OrderStatus? status)
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        ViewBag.SelectedStatus = status;
        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    public async Task<IActionResult> Create()
    {
        var viewModel = new OrderFormViewModel
        {
            CustomerOptions = await BuildCustomerOptions(),
            ProductOptions = await BuildProductOptions(),
        };

        for (var i = 0; i < EmptyItemRowCount; i++)
        {
            viewModel.Items.Add(new OrderItemInputModel());
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderFormViewModel viewModel)
    {
        List<OrderItem> items;
        try
        {
            items = await BuildOrderItems(viewModel.Items);
        }
        catch (OrderItemValidationException ex)
        {
            items = new List<OrderItem>();
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        if (items.Count == 0 && ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Add at least one line item with a product and quantity.");
        }

        if (!ModelState.IsValid)
        {
            viewModel.CustomerOptions = await BuildCustomerOptions();
            viewModel.ProductOptions = await BuildProductOptions();
            return View(viewModel);
        }

        var order = new Order
        {
            CustomerId = viewModel.CustomerId!.Value,
            OrderDate = DateTime.SpecifyKind(viewModel.OrderDate, DateTimeKind.Utc),
            Status = viewModel.Status,
            Notes = viewModel.Notes,
            OrderItems = items,
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"Order #{order.Id} created.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        var viewModel = new OrderFormViewModel
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            OrderDate = order.OrderDate,
            Status = order.Status,
            Notes = order.Notes,
            CustomerOptions = await BuildCustomerOptions(),
            ProductOptions = await BuildProductOptions(),
        };

        foreach (var item in order.OrderItems)
        {
            viewModel.Items.Add(new OrderItemInputModel { ProductId = item.ProductId, Quantity = item.Quantity });
        }

        for (var i = 0; i < EmptyItemRowCount; i++)
        {
            viewModel.Items.Add(new OrderItemInputModel());
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OrderFormViewModel viewModel)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        List<OrderItem> items;
        try
        {
            items = await BuildOrderItems(viewModel.Items);
        }
        catch (OrderItemValidationException ex)
        {
            items = new List<OrderItem>();
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        if (items.Count == 0 && ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Add at least one line item with a product and quantity.");
        }

        if (!ModelState.IsValid)
        {
            viewModel.Id = id;
            viewModel.CustomerOptions = await BuildCustomerOptions();
            viewModel.ProductOptions = await BuildProductOptions();
            return View(viewModel);
        }

        order.CustomerId = viewModel.CustomerId!.Value;
        order.OrderDate = DateTime.SpecifyKind(viewModel.OrderDate, DateTimeKind.Utc);
        order.Status = viewModel.Status;
        order.Notes = viewModel.Notes;

        order.OrderItems.Clear();
        foreach (var item in items)
        {
            order.OrderItems.Add(item);
        }

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = $"Order #{order.Id} updated.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    private async Task<List<OrderItem>> BuildOrderItems(List<OrderItemInputModel> inputs)
    {
        var items = new List<OrderItem>();

        var relevant = inputs.Where(i => i.ProductId.HasValue && i.Quantity > 0).ToList();
        if (relevant.Count == 0)
        {
            return items;
        }

        var productIds = relevant.Select(i => i.ProductId!.Value).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var input in relevant)
        {
            if (!products.TryGetValue(input.ProductId!.Value, out var product))
            {
                // Every product option in the form is populated straight from
                // the catalog, so this should be unreachable through normal
                // use. Reject the submission instead of silently creating a
                // line item with no price attached to it.
                throw new OrderItemValidationException(
                    $"Product {input.ProductId} could not be found. Refresh the page and try again.");
            }

            items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                UnitPrice = product.Price,
                Quantity = input.Quantity,
            });
        }

        return items;
    }

    private async Task<List<SelectListItem>> BuildCustomerOptions()
    {
        return await _context.Customers
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> BuildProductOptions()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Name + " — $" + p.Price })
            .ToListAsync();
    }
}

using OrderFlow.Models;

namespace OrderFlow.Data;

public static class DbInitializer
{
    public static void Seed(OrderFlowContext context)
    {
        if (context.Customers.Any())
        {
            return;
        }

        var customers = new List<Customer>
        {
            new() { Name = "Maria Ilic", Email = "maria.ilic@example.com", Phone = "555-0101" },
            new() { Name = "Devon Marsh", Email = "devon.marsh@example.com", Phone = "555-0102" },
            new() { Name = "Priya Natarajan", Email = "priya.n@example.com", Phone = "555-0103" },
            new() { Name = "Tom Whitfield", Email = "tom.whitfield@example.com", Phone = "555-0104" },
            new() { Name = "Aiko Sato", Email = "aiko.sato@example.com", Phone = "555-0105" },
        };
        context.Customers.AddRange(customers);

        var products = new List<Product>
        {
            new() { Name = "Canvas Weekender Bag", Sku = "SKU-1001", Price = 89.00m },
            new() { Name = "Ceramic Pour-Over Kettle", Sku = "SKU-1002", Price = 54.50m },
            new() { Name = "Merino Wool Throw", Sku = "SKU-1003", Price = 129.00m },
            new() { Name = "Cast Iron Skillet 10\"", Sku = "SKU-1004", Price = 42.00m },
            new() { Name = "Desk Lamp, Walnut Base", Sku = "SKU-1005", Price = 76.00m },
            new() { Name = "Recycled Wool Rug 5x7", Sku = "SKU-1006", Price = 310.00m },
            new() { Name = "Insulated Travel Mug", Sku = "SKU-1007", Price = 28.00m },
            new() { Name = "Linen Napkin Set (4)", Sku = "SKU-1008", Price = 36.00m },
        };
        context.Products.AddRange(products);
        context.SaveChanges();

        var today = DateTime.UtcNow.Date;

        var orders = new List<Order>
        {
            new()
            {
                CustomerId = customers[0].Id,
                OrderDate = today.AddDays(-21),
                Status = OrderStatus.Completed,
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = products[0].Id, ProductNameSnapshot = products[0].Name, UnitPrice = products[0].Price, Quantity = 1 },
                    new() { ProductId = products[6].Id, ProductNameSnapshot = products[6].Name, UnitPrice = products[6].Price, Quantity = 2 },
                }
            },
            new()
            {
                CustomerId = customers[1].Id,
                OrderDate = today.AddDays(-18),
                Status = OrderStatus.Shipped,
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = products[2].Id, ProductNameSnapshot = products[2].Name, UnitPrice = products[2].Price, Quantity = 1 },
                }
            },
            new()
            {
                CustomerId = customers[2].Id,
                OrderDate = today.AddDays(-14),
                Status = OrderStatus.Completed,
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = products[3].Id, ProductNameSnapshot = products[3].Name, UnitPrice = products[3].Price, Quantity = 1 },
                    new() { ProductId = products[7].Id, ProductNameSnapshot = products[7].Name, UnitPrice = products[7].Price, Quantity = 2 },
                }
            },
            new()
            {
                CustomerId = customers[3].Id,
                OrderDate = today.AddDays(-11),
                Status = OrderStatus.Processing,
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = products[4].Id, ProductNameSnapshot = products[4].Name, UnitPrice = products[4].Price, Quantity = 1 },
                }
            },
            new()
            {
                CustomerId = customers[4].Id,
                OrderDate = today.AddDays(-9),
                Status = OrderStatus.Completed,
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = products[5].Id, ProductNameSnapshot = products[5].Name, UnitPrice = products[5].Price, Quantity = 1 },
                }
            },
            new()
            {
                CustomerId = customers[0].Id,
                OrderDate = today.AddDays(-7),
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = products[1].Id, ProductNameSnapshot = products[1].Name, UnitPrice = products[1].Price, Quantity = 1 },
                    new() { ProductId = products[6].Id, ProductNameSnapshot = products[6].Name, UnitPrice = products[6].Price, Quantity = 3 },
                }
            },
            new()
            {
                CustomerId = customers[2].Id,
                OrderDate = today.AddDays(-5),
                Status = OrderStatus.Shipped,
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = products[0].Id, ProductNameSnapshot = products[0].Name, UnitPrice = products[0].Price, Quantity = 2 },
                }
            },
            new()
            {
                CustomerId = customers[3].Id,
                OrderDate = today.AddDays(-3),
                Status = OrderStatus.Completed,
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = products[3].Id, ProductNameSnapshot = products[3].Name, UnitPrice = products[3].Price, Quantity = 2 },
                    new() { ProductId = products[4].Id, ProductNameSnapshot = products[4].Name, UnitPrice = products[4].Price, Quantity = 1 },
                }
            },
            new()
            {
                CustomerId = customers[4].Id,
                OrderDate = today.AddDays(-1),
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = products[7].Id, ProductNameSnapshot = products[7].Name, UnitPrice = products[7].Price, Quantity = 4 },
                }
            },
            // Legacy POS import batch: the matching script links order lines to
            // catalog products by SKU. "Bamboo Cutting Board" was discontinued
            // and removed from the catalog before this batch ran, so its SKU no
            // longer matches anything -- the row still needs to be preserved for
            // historical reporting, so it comes through with ProductId left null
            // instead of being dropped. The import file itself still had a price
            // for the line ($24.00), which is what's captured in UnitPrice below;
            // that price was always available; the bug was that the original
            // code never captured it independently of the (missing) product link.
            new()
            {
                CustomerId = customers[1].Id,
                OrderDate = today.AddDays(-30),
                Status = OrderStatus.Completed,
                Notes = "Imported from legacy POS system (batch #2024-11-quarterly).",
                OrderItems = new List<OrderItem>
                {
                    new() { ProductId = products[2].Id, ProductNameSnapshot = products[2].Name, UnitPrice = products[2].Price, Quantity = 1 },
                    new() { ProductId = null, ProductNameSnapshot = "Bamboo Cutting Board (legacy SKU LEG-4471, discontinued)", UnitPrice = 24.00m, Quantity = 2 },
                }
            },
        };

        context.Orders.AddRange(orders);
        context.SaveChanges();
    }
}

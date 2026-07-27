using Microsoft.EntityFrameworkCore;
using OrderFlow.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<OrderFlowContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("OrderFlowContext")
        ?? "Data Source=orderflow.db"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderFlowContext>();
    context.Database.Migrate();
    DbInitializer.Seed(context);
}

app.Run();

public partial class Program { }

using EfCoreExporter;
using Microsoft.EntityFrameworkCore;

// Writes the model to the file named by the first argument, or to standard output without one,
// ready to pipe into pondhawk-diagram:
//   dotnet run --project examples/EfCoreExporter | pondhawk-diagram - -o shop.html

using var context = new ShopContext();

var model = EfCoreDiagramExporter.Export(context, name: "Shop", description: "Exported from the EF Core model");

var errors = model.Validate();
if (errors.Count > 0)
{
    foreach (var error in errors)
        Console.Error.WriteLine(error);
    return 1;
}

if (args.Length > 0)
    File.WriteAllText(args[0], model.ToJson());
else
    Console.WriteLine(model.ToJson());
return 0;

// A small model that exercises what an exporter has to cope with: an enum, an owned type,
// a many-to-many relationship, an alternate key, a comment and a view.
public sealed class ShopContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();

    // The provider supplies column types; the connection is never opened.
    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite("Data Source=:memory:");

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Customer>(customer =>
        {
            customer.HasAlternateKey(c => c.Email);
            customer.OwnsOne(c => c.Address);
        });

        model.Entity<Order>(order =>
        {
            order.ToTable(t => t.HasComment("One row per checkout"));
            order.HasIndex(o => o.PlacedAt);
            order.HasMany(o => o.Products).WithMany().UsingEntity("OrderProducts");
        });

        model.Entity<OrderTotal>().HasNoKey().ToView("OrderTotals");
    }
}

public sealed class Customer
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public string? Name { get; set; }
    public Address? Address { get; set; }
}

public sealed class Address
{
    public required string City { get; set; }
    public required string PostCode { get; set; }
}

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime PlacedAt { get; set; }
    public List<Product> Products { get; set; } = [];
}

public sealed class Product
{
    public int Id { get; set; }
    public required string Sku { get; set; }
}

public sealed class OrderTotal
{
    public int OrderId { get; set; }
    public int Quantity { get; set; }
}

public enum OrderStatus { Open, Paid, Shipped, Cancelled }

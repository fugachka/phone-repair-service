using Microsoft.EntityFrameworkCore;
using PhoneRepairService.Models;

namespace PhoneRepairService.Data;

/// <summary>
/// Контекст Entity Framework Core: отображение классов предметной области на таблицы MySQL.
/// User и наследники Admin, Master, Client хранятся в одной таблице Users (тип — столбец Role),
/// связь «многие ко многим» Order — Fault хранится в таблице OrderFaults.
/// </summary>
public class RepairDbContext : DbContext
{
    public RepairDbContext(DbContextOptions<RepairDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PhoneModel> PhoneModels => Set<PhoneModel>();
    public DbSet<Fault> Faults => Set<Fault>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Ignore(u => u.Role);
            e.HasDiscriminator<string>("UserRole")
                .HasValue<Admin>(Roles.Admin)
                .HasValue<Master>(Roles.Master)
                .HasValue<Client>(Roles.Client);
            e.Property<string>("UserRole").HasColumnName("Role").HasMaxLength(20);
            e.Property(u => u.Login).IsRequired().HasMaxLength(50);
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
            e.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            e.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(20);
            e.HasIndex(u => u.Login).IsUnique();
            e.HasIndex(u => u.PhoneNumber);
        });
        modelBuilder.Entity<Client>().Property(c => c.Email).HasMaxLength(254);
        modelBuilder.Entity<Master>().Property(m => m.Specialization).HasMaxLength(100);

        modelBuilder.Entity<PhoneModel>(e =>
        {
            e.ToTable("PhoneModels");
            e.Property(m => m.Brand).IsRequired().HasMaxLength(100);
            e.Property(m => m.ModelName).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Fault>(e =>
        {
            e.ToTable("Faults", t => t.HasCheckConstraint("CK_Faults_BasePrice", "BasePrice >= 0"));
            e.Property(f => f.Title).IsRequired().HasMaxLength(200);
            e.Property(f => f.BasePrice).HasPrecision(10, 2);
            e.HasIndex(f => f.Title).IsUnique();
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders", t =>
            {
                t.HasCheckConstraint("CK_Orders_Status", "Status BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_Orders_TotalPrice", "TotalPrice >= 0");
            });
            e.Property(o => o.Imei).IsRequired().HasMaxLength(15);
            e.Property(o => o.TotalPrice).HasPrecision(10, 2);
            e.HasOne(o => o.Client).WithMany().HasForeignKey(o => o.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.PhoneModel).WithMany().HasForeignKey(o => o.PhoneModelId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Master).WithMany().HasForeignKey(o => o.MasterId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(o => o.Faults).WithMany().UsingEntity<Dictionary<string, object>>(
                "OrderFaults",
                right => right.HasOne<Fault>().WithMany().HasForeignKey("FaultId").OnDelete(DeleteBehavior.Restrict),
                left => left.HasOne<Order>().WithMany().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade),
                join => join.HasKey("OrderId", "FaultId"));
            e.HasIndex(o => o.ClientId);
            e.HasIndex(o => o.MasterId);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using PhoneRepairService.Data;
using PhoneRepairService.Models;
using PhoneRepairService.Services;

namespace PhoneRepairService.Tests;

/// <summary>Фиксированное время для проверки дат (statusChangedDate, creationDate).</summary>
public sealed class FixedTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 15, 10, 30, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

/// <summary>
/// Отдельная временная база данных MySQL для теста: создаётся по миграциям приложения,
/// заполняется начальными данными и удаляется после теста.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    /// <summary>
    /// Подключение к серверу MySQL для тестов. Можно переопределить переменной окружения PHONE_REPAIR_TEST_DB.
    /// </summary>
    public static string ServerConnectionString =>
        Environment.GetEnvironmentVariable("PHONE_REPAIR_TEST_DB")
        ?? "server=localhost;port=3306;user=phone_repair;password=phone_repair_pass;CharSet=utf8mb4;AllowPublicKeyRetrieval=true";

    public string ConnectionString { get; }
    public RepairDb Db { get; }
    public FixedTimeProvider Time { get; } = new();
    public AdminService Admin { get; }
    public MasterService Masters { get; }
    public ClientService Clients { get; }
    public AuthService Auth { get; }

    private readonly RepairDbContext _context;

    public TestDatabase()
    {
        ConnectionString = new MySqlConnectionStringBuilder(ServerConnectionString)
        {
            Database = $"phone_repair_test_{Guid.NewGuid():N}"[..40]
        }.ConnectionString;
        _context = CreateContext(ConnectionString);
        DbInitializer.Initialize(_context, seedDemoData: true);
        Db = new RepairDb(_context);
        Admin = new AdminService(Db, Time);
        Masters = new MasterService(Db, Time);
        Clients = new ClientService(Db);
        Auth = new AuthService(Db);
    }

    public Client Ivanov => (Client)Db.FindUserByLogin("ivanov")!;
    public Master Master1 => (Master)Db.FindUserByLogin("master1")!;
    public Master Master2 => (Master)Db.FindUserByLogin("master2")!;
    public PhoneModel ActiveModel => Db.GetPhoneModels(activeOnly: true).First(m => m.ModelName == "iPhone 12");
    public PhoneModel InactiveModel => Db.GetPhoneModels().First(m => !m.IsActive);
    public Fault Fault(string title) => Db.GetFaults().First(f => f.Title == title);

    /// <summary>Оформляет заказ для клиента «ivanov» с двумя неисправностями (4500 + 2500).</summary>
    public Order CreateOrder(string imei = "356938035643809", Client? client = null) =>
        Admin.CreateOrder(client ?? Ivanov, ActiveModel,
            new List<Fault> { Fault("Замена дисплея"), Fault("Замена аккумулятора") }, imei);

    /// <summary>Новый контекст для той же БД (чтобы перечитать данные через новое подключение).</summary>
    public static RepairDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<RepairDbContext>().UseMySQL(connectionString).Options);

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

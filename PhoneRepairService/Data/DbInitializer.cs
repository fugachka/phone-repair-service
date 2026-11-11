using Microsoft.EntityFrameworkCore;
using PhoneRepairService.Models;
using PhoneRepairService.Services;

namespace PhoneRepairService.Data;

/// <summary>
/// Создаёт или обновляет БД по миграциям и заполняет начальные данные при первом запуске.
/// Учётные записи сотрудников (администратора и мастеров) по ПЗ создаются вне системы —
/// здесь они заводятся начальным заполнением БД.
/// </summary>
public static class DbInitializer
{
    public static void Initialize(RepairDbContext context, bool seedDemoData)
    {
        context.Database.Migrate();
        if (!seedDemoData || context.Users.Any()) return;

        var db = new RepairDb(context);

        db.InTransaction(() =>
        {
            db.InsertUser(new Admin
            {
                Login = "admin",
                PasswordHash = PasswordHashing.Hash("admin123"),
                FullName = "Смирнова Анна Сергеевна",
                PhoneNumber = "+79000000001"
            });
            db.InsertUser(new Master
            {
                Login = "master1",
                PasswordHash = PasswordHashing.Hash("master123"),
                FullName = "Петров Пётр Петрович",
                PhoneNumber = "+79000000002",
                Specialization = "Смартфоны Apple"
            });
            db.InsertUser(new Master
            {
                Login = "master2",
                PasswordHash = PasswordHashing.Hash("master123"),
                FullName = "Кузнецов Алексей Викторович",
                PhoneNumber = "+79000000003",
                Specialization = "Смартфоны на Android"
            });
            db.InsertUser(new Client
            {
                Login = "ivanov",
                PasswordHash = PasswordHashing.Hash("client123"),
                FullName = "Иванов Иван Иванович",
                PhoneNumber = "+79001234567",
                Email = "ivanov@example.com"
            });

            foreach (var (brand, model, active) in new[]
            {
                ("Apple", "iPhone 12", true),
                ("Apple", "iPhone 13", true),
                ("Apple", "iPhone 7", false),
                ("Samsung", "Galaxy S21", true),
                ("Samsung", "Galaxy A52", true),
                ("Xiaomi", "Redmi Note 10", true)
            })
                db.InsertPhoneModel(new PhoneModel { Brand = brand, ModelName = model, IsActive = active });

            foreach (var (title, price) in new[]
            {
                ("Замена дисплея", 4500m),
                ("Замена аккумулятора", 2500m),
                ("Замена разъёма зарядки", 1800m),
                ("Замена основной камеры", 3000m),
                ("Ремонт после попадания влаги", 3500m)
            })
                db.InsertFault(new Fault { Title = title, BasePrice = price });
            return 0;
        });
    }
}

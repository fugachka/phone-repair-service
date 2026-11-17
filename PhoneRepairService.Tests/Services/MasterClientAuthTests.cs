using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using PhoneRepairService.Data;
using PhoneRepairService.Models;
using PhoneRepairService.Services;

namespace PhoneRepairService.Tests.Services;

/// <summary>Операции мастера с базой данных: Т.2 «Указать диагноз», Т.3 «Изменить статус заказа».</summary>
[TestFixture]
public class MasterServiceTests
{
    [Test, Category("Т.3")]
    [Description("«Принят в ремонт» → «На диагностике» связывает заказ с мастером и сохраняет время изменения")]
    public void UpdateStatus_ToDiagnostics_LinksMaster()
    {
        using var t = new TestDatabase();
        var order = t.CreateOrder();
        t.Time.Now = new DateTimeOffset(2026, 11, 16, 9, 15, 42, TimeSpan.Zero);

        t.Masters.UpdateOrderStatus(t.Master1, order.Id, OrderStatus.InDiagnostics);

        var stored = t.Db.GetOrder(order.Id)!;
        Assert.That(stored.Status, Is.EqualTo(OrderStatus.InDiagnostics));
        Assert.That(stored.MasterId, Is.EqualTo(t.Master1.Id));
        Assert.That(stored.StatusChangedDate, Is.EqualTo(new DateTime(2026, 11, 16, 9, 15, 42)));
    }

    [Test, Category("Т.3")]
    [Description("Полный путь «Принят» → «На диагностике» → «В процессе ремонта» → «Готов к выдаче»")]
    public void UpdateStatus_FullLifecycle()
    {
        using var t = new TestDatabase();
        var order = t.CreateOrder();
        t.Masters.UpdateOrderStatus(t.Master1, order.Id, OrderStatus.InDiagnostics);
        t.Masters.UpdateOrderStatus(t.Master2, order.Id, OrderStatus.InRepair);
        t.Masters.UpdateOrderStatus(t.Master2, order.Id, OrderStatus.Ready);

        var stored = t.Db.GetOrder(order.Id)!;
        Assert.That(stored.Status, Is.EqualTo(OrderStatus.Ready));
        Assert.That(stored.MasterId, Is.EqualTo(t.Master1.Id));
    }

    [Test, Category("Т.3")]
    [Description("Недопустимый переход отклоняется, статус не меняется")]
    public void UpdateStatus_ForbiddenTransition()
    {
        using var t = new TestDatabase();
        var order = t.CreateOrder();
        var ex = Assert.Catch<BusinessRuleException>(() =>
            t.Masters.UpdateOrderStatus(t.Master1, order.Id, OrderStatus.Ready));
        Assert.That(ex!.Message, Does.Contain("недопустим"));
        var stored = t.Db.GetOrder(order.Id)!;
        Assert.That(stored.Status, Is.EqualTo(OrderStatus.Created));
        Assert.That(stored.StatusChangedDate, Is.Null);
        Assert.That(stored.MasterId, Is.Null);
    }

    [Test, Category("Т.3")]
    [Description("Конечный статус «Отменён» больше не изменяется")]
    public void UpdateStatus_FinalStatusIsFinal()
    {
        using var t = new TestDatabase();
        var order = t.CreateOrder();
        t.Masters.UpdateOrderStatus(t.Master1, order.Id, OrderStatus.Cancelled);
        Assert.That(t.Db.GetOrder(order.Id)!.MasterId, Is.Null);

        var ex = Assert.Catch<BusinessRuleException>(() =>
            t.Masters.UpdateOrderStatus(t.Master1, order.Id, OrderStatus.InDiagnostics));
        Assert.That(ex!.Message, Does.Contain("конечный"));
        Assert.That(t.Db.GetOrder(order.Id)!.Status, Is.EqualTo(OrderStatus.Cancelled));
    }

    [Test, Category("Т.2")]
    [Description("SetDiagnosis() сохраняет диагноз, статус и стоимость не меняются")]
    public void SetDiagnosis_DoesNotChangeStatus()
    {
        using var t = new TestDatabase();
        var order = t.CreateOrder();
        t.Masters.UpdateOrderStatus(t.Master1, order.Id, OrderStatus.InDiagnostics);
        var changedAt = t.Db.GetOrder(order.Id)!.StatusChangedDate;
        t.Time.Now = t.Time.Now.AddHours(1);

        t.Masters.SetDiagnosis(t.Master1, order.Id, "Разбит дисплейный модуль, вздут аккумулятор");

        var stored = t.Db.GetOrder(order.Id)!;
        Assert.That(stored.Diagnosis, Is.EqualTo("Разбит дисплейный модуль, вздут аккумулятор"));
        Assert.That(stored.Status, Is.EqualTo(OrderStatus.InDiagnostics));
        Assert.That(stored.StatusChangedDate, Is.EqualTo(changedAt));
        Assert.That(stored.TotalPrice, Is.EqualTo(7000m));
    }

    [Test, Category("Т.2"), Category("Т.3")]
    [Description("Альт.: заключение о неремонтопригодности сохраняется как диагноз, отказ — через Т.3")]
    public void SetDiagnosis_NotRepairable_ThenCancel()
    {
        using var t = new TestDatabase();
        var order = t.CreateOrder();
        t.Masters.UpdateOrderStatus(t.Master1, order.Id, OrderStatus.InDiagnostics);
        t.Masters.SetDiagnosis(t.Master1, order.Id, "Не подлежит ремонту: критическое повреждение платы после затопления");
        Assert.That(t.Db.GetOrder(order.Id)!.Status, Is.EqualTo(OrderStatus.InDiagnostics));

        t.Masters.UpdateOrderStatus(t.Master1, order.Id, OrderStatus.Cancelled);
        var stored = t.Db.GetOrder(order.Id)!;
        Assert.That(stored.Status, Is.EqualTo(OrderStatus.Cancelled));
        Assert.That(stored.Diagnosis, Does.Contain("Не подлежит ремонту"));
    }

    [Test, Category("Т.2")]
    [Description("Диагноз указывается только в статусе «На диагностике» и не может быть пустым")]
    public void SetDiagnosis_Preconditions()
    {
        using var t = new TestDatabase();
        var order = t.CreateOrder();
        Assert.Catch<BusinessRuleException>(() => t.Masters.SetDiagnosis(t.Master1, order.Id, "Диагноз"));
        t.Masters.UpdateOrderStatus(t.Master1, order.Id, OrderStatus.InDiagnostics);
        var ex = Assert.Catch<BusinessRuleException>(() => t.Masters.SetDiagnosis(t.Master1, order.Id, "   "));
        Assert.That(ex!.Field, Is.EqualTo("DiagnosisText"));
        Assert.That(t.Db.GetOrder(order.Id)!.Diagnosis, Is.Null);
    }

    [Test, Category("Т.2"), Category("Т.3")]
    [Description("Операции мастера недоступны пользователю другой роли")]
    public void MasterOperations_RequireMaster()
    {
        using var t = new TestDatabase();
        var order = t.CreateOrder();
        var fakeMaster = new Master { Id = t.Ivanov.Id };
        Assert.Catch<BusinessRuleException>(() =>
            t.Masters.UpdateOrderStatus(fakeMaster, order.Id, OrderStatus.InDiagnostics));
        Assert.That(t.Db.GetOrder(order.Id)!.Status, Is.EqualTo(OrderStatus.Created));
    }
}

/// <summary>Т.1 «Просмотреть свои заказы», Т.11 «Просмотреть карточку заказа», Т.10 «Авторизоваться».</summary>
[TestFixture]
public class ClientAndAuthServiceTests
{
    [Test, Category("Т.1")]
    [Description("ViewMyOrders() возвращает только заказы этого клиента с текущими статусами")]
    public void ViewMyOrders_OnlyOwnOrders()
    {
        using var t = new TestDatabase();
        var other = t.Admin.RegisterClient("Сидорова М.О.", "+79125554433", "m@example.com", "sidorova", "pass1234");
        var mine = t.CreateOrder("111111111111111");
        t.CreateOrder("222222222222222", other);
        t.Masters.UpdateOrderStatus(t.Master1, mine.Id, OrderStatus.InDiagnostics);

        var orders = t.Clients.ViewMyOrders(t.Ivanov);
        Assert.That(orders, Has.Count.EqualTo(1));
        Assert.That(orders[0].Id, Is.EqualTo(mine.Id));
        Assert.That(orders[0].Status, Is.EqualTo(OrderStatus.InDiagnostics));
    }

    [Test, Category("Т.1")]
    [Description("Альт.: у нового клиента заказов нет")]
    public void ViewMyOrders_Empty()
    {
        using var t = new TestDatabase();
        Assert.That(t.Clients.ViewMyOrders(t.Ivanov), Is.Empty);
    }

    [Test, Category("Т.1"), Category("Т.11")]
    [Description("Альт.: чужой заказ клиенту не показывается")]
    public void GetMyOrder_ForeignOrderHidden()
    {
        using var t = new TestDatabase();
        var other = t.Admin.RegisterClient("Сидорова М.О.", "+79125554433", "m@example.com", "sidorova", "pass1234");
        var foreign = t.CreateOrder("222222222222222", other);
        Assert.That(t.Clients.GetMyOrder(t.Ivanov, foreign.Id), Is.Null);
        Assert.That(t.Clients.GetMyOrder(other, foreign.Id), Is.Not.Null);
    }

    [Category("Т.10")]
    [Description("Вход с верным паролем — пользователь получает свою роль")]
    [TestCase("admin", "admin123", Roles.Admin)]
    [TestCase("master1", "master123", Roles.Master)]
    [TestCase("ivanov", "client123", Roles.Client)]
    [TestCase("IVANOV", "client123", Roles.Client)]
    public void Authenticate_Success(string login, string password, string role)
    {
        using var t = new TestDatabase();
        var user = t.Auth.Authenticate(login, password);
        Assert.That(user, Is.Not.Null);
        Assert.That(user!.Role, Is.EqualTo(role));
    }

    [Category("Т.10")]
    [Description("Альт.: неверный логин или пароль")]
    [TestCase("admin", "wrong")]
    [TestCase("nobody", "admin123")]
    [TestCase("", "")]
    public void Authenticate_Failure(string login, string password)
    {
        using var t = new TestDatabase();
        Assert.That(t.Auth.Authenticate(login, password), Is.Null);
    }
}

/// <summary>Проверки базы данных: миграции, начальное заполнение, сохранение данных, ограничения.</summary>
[TestFixture]
public class DatabaseTests
{
    [Test]
    [Description("Начальное заполнение — администратор, 2 мастера, клиент, 6 моделей, 5 неисправностей")]
    public void Seed_CreatesStaffAndCatalogs()
    {
        using var t = new TestDatabase();
        Assert.That(t.Db.FindUserByLogin("admin")!.Role, Is.EqualTo(Roles.Admin));
        Assert.That(t.Master1.Specialization, Is.EqualTo("Смартфоны Apple"));
        Assert.That(t.Master2.Role, Is.EqualTo(Roles.Master));
        Assert.That(t.Db.GetClients(), Has.Count.EqualTo(1));
        Assert.That(t.Db.GetPhoneModels(), Has.Count.EqualTo(6));
        Assert.That(t.Db.GetPhoneModels(activeOnly: true), Has.Count.EqualTo(5));
        Assert.That(t.Db.GetFaults(), Has.Count.EqualTo(5));
    }

    [Test]
    [Description("Повторная инициализация не дублирует данные и не требует новых миграций")]
    public void Initialize_IsIdempotent()
    {
        using var t = new TestDatabase();
        using (var again = TestDatabase.CreateContext(t.ConnectionString))
        {
            DbInitializer.Initialize(again, seedDemoData: true);
            Assert.That(again.Database.GetPendingMigrations(), Is.Empty);
        }
        using var check = TestDatabase.CreateContext(t.ConnectionString);
        Assert.That(check.Users.Count(), Is.EqualTo(4));
        Assert.That(check.Faults.Count(), Is.EqualTo(5));
    }

    [Test]
    [Description("Данные заказа сохраняются в БД и читаются через новое подключение")]
    public void Data_PersistsAfterReopen()
    {
        using var t = new TestDatabase();
        var order = t.CreateOrder();
        t.Masters.UpdateOrderStatus(t.Master1, order.Id, OrderStatus.InDiagnostics);
        t.Masters.SetDiagnosis(t.Master1, order.Id, "Замена шлейфа");

        using var context = TestDatabase.CreateContext(t.ConnectionString);
        var stored = new RepairDb(context).GetOrder(order.Id);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.Diagnosis, Is.EqualTo("Замена шлейфа"));
        Assert.That(stored.Status, Is.EqualTo(OrderStatus.InDiagnostics));
        Assert.That(stored.Master!.FullName, Is.EqualTo("Петров Пётр Петрович"));
        Assert.That(stored.Faults, Has.Count.EqualTo(2));
        Assert.That(stored.TotalPrice, Is.EqualTo(7000m));
    }

    [Test]
    [Description("Ограничения целостности в БД: уникальный логин, внешний ключ, допустимый статус")]
    public void Constraints_Enforced()
    {
        using var t = new TestDatabase();
        var database = t.Db.Context.Database;

        var ex = Assert.Catch<MySqlException>(() => database.ExecuteSqlRaw(
            "INSERT INTO Users (Role, Login, PasswordHash, FullName, PhoneNumber) VALUES ('Client', 'admin', 'x', 'x', 'x')"));
        Assert.That(ex!.Number, Is.EqualTo(1062), "повтор уникального логина");

        ex = Assert.Catch<MySqlException>(() => database.ExecuteSqlRaw(
            "INSERT INTO Orders (Imei, CreationDate, Status, TotalPrice, ClientId, PhoneModelId) " +
            "VALUES ('1', '2026-01-01 00:00:00', 0, 0, 999, 1)"));
        Assert.That(ex!.Number, Is.EqualTo(1452), "ссылка на несуществующего клиента");

        string badStatus = "INSERT INTO Orders (Imei, CreationDate, Status, TotalPrice, ClientId, PhoneModelId) " +
                           "VALUES ('1', '2026-01-01 00:00:00', 7, 0, " + t.Ivanov.Id + ", 1)";
        ex = Assert.Catch<MySqlException>(() => database.ExecuteSqlRaw(badStatus));
        Assert.That(ex!.Number, Is.EqualTo(3819), "статус вне диапазона 0–4");
    }
}

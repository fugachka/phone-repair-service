using PhoneRepairService.Models;
using PhoneRepairService.Services;

namespace PhoneRepairService.Tests.Services;

/// <summary>Операции администратора с базой данных MySQL: Т.4, Т.5, Т.6, Т.8, Т.9.</summary>
[TestFixture]
public class AdminServiceTests
{
    [Test, Category("Т.5")]
    [Description("RegisterClient() сохраняет клиента и учётную запись с хэшем пароля")]
    public void RegisterClient_SavesClientWithHashedPassword()
    {
        using var t = new TestDatabase();
        var client = t.Admin.RegisterClient("Сидорова Мария Олеговна", "8 (912) 555-44-33", "maria@example.com", "sidorova", "pass1234");

        var stored = t.Db.GetClient(client.Id);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.FullName, Is.EqualTo("Сидорова Мария Олеговна"));
        Assert.That(stored.PhoneNumber, Is.EqualTo("+79125554433"));
        Assert.That(stored.Email, Is.EqualTo("maria@example.com"));
        Assert.That(stored.Role, Is.EqualTo(Roles.Client));
        Assert.That(stored.PasswordHash, Is.Not.EqualTo("pass1234"));
        Assert.That(t.Auth.Authenticate("sidorova", "wrong"), Is.Null);
        Assert.That(t.Auth.Authenticate("sidorova", "pass1234"), Is.Not.Null);
    }

    [Test, Category("Т.5")]
    [Description("Альт.: клиент с таким номером уже зарегистрирован — возвращается найденный клиент")]
    public void RegisterClient_DuplicatePhone_ReturnsExistingClient()
    {
        using var t = new TestDatabase();
        var ex = Assert.Throws<DuplicateClientPhoneException>(() =>
            t.Admin.RegisterClient("Иванов И.И.", "8 900 123-45-67", "other@example.com", "ivanov2", "pass1234"));
        Assert.That(ex!.Message, Is.EqualTo("Клиент с таким номером уже зарегистрирован"));
        Assert.That(ex.ExistingClient.Id, Is.EqualTo(t.Ivanov.Id));
        Assert.That(t.Db.GetClients(), Has.Count.EqualTo(1));
    }

    [Test, Category("Т.5")]
    [Description("Альт.: логин занят — после ввода другого логина регистрация проходит")]
    public void RegisterClient_DuplicateLogin_ThenAnotherLogin()
    {
        using var t = new TestDatabase();
        var ex = Assert.Throws<DuplicateLoginException>(() =>
            t.Admin.RegisterClient("Орлов Олег", "+79115550000", "orlov@example.com", "IVANOV", "pass1234"));
        Assert.That(ex!.Message, Does.Contain("уже занят"));

        var client = t.Admin.RegisterClient("Орлов Олег", "+79115550000", "orlov@example.com", "orlov", "pass1234");
        Assert.That(client.Id, Is.GreaterThan(0));
        Assert.That(t.Db.GetClients(), Has.Count.EqualTo(2));
    }

    [Category("Т.5")]
    [Description("Обязательные данные клиента проверяются")]
    [TestCase("", "+79115550001", "a@b.ru", "login1", "pass1234", "FullName")]
    [TestCase("Петров П.", "123", "a@b.ru", "login1", "pass1234", "Phone")]
    [TestCase("Петров П.", "+79115550001", "не почта", "login1", "pass1234", "Email")]
    [TestCase("Петров П.", "+79115550001", "a@b.ru", "логин", "pass1234", "Login")]
    [TestCase("Петров П.", "+79115550001", "a@b.ru", "login1", "123", "Password")]
    public void RegisterClient_Validation(string name, string phone, string email, string login, string password, string field)
    {
        using var t = new TestDatabase();
        var ex = Assert.Catch<BusinessRuleException>(() => t.Admin.RegisterClient(name, phone, email, login, password));
        Assert.That(ex!.Field, Is.EqualTo(field));
    }

    [Test, Category("Т.4")]
    [Description("CreateOrder() — статус «Принят в ремонт», мастер не назначен, стоимость рассчитана")]
    public void CreateOrder_InitialState()
    {
        using var t = new TestDatabase();
        var order = t.CreateOrder();

        var stored = t.Db.GetOrder(order.Id);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.Status, Is.EqualTo(OrderStatus.Created));
        Assert.That(stored.MasterId, Is.Null);
        Assert.That(stored.StatusChangedDate, Is.Null);
        Assert.That(stored.Diagnosis, Is.Null);
        Assert.That(stored.TotalPrice, Is.EqualTo(7000m));
        Assert.That(stored.Imei, Is.EqualTo("356938035643809"));
        Assert.That(stored.CreationDate, Is.EqualTo(new DateTime(2026, 11, 15, 10, 30, 0)));
        Assert.That(stored.Client.Id, Is.EqualTo(t.Ivanov.Id));
        Assert.That(stored.PhoneModel.GetFullName(), Is.EqualTo("Apple iPhone 12"));
        Assert.That(stored.Faults, Has.Count.EqualTo(2));
    }

    [Test, Category("Т.4")]
    [Description("Каждый заказ получает уникальный номер")]
    public void CreateOrder_UniqueNumbers()
    {
        using var t = new TestDatabase();
        var a = t.CreateOrder("111111111111111");
        var b = t.CreateOrder("222222222222222");
        Assert.That(a.Id, Is.GreaterThan(0));
        Assert.That(b.Id, Is.Not.EqualTo(a.Id));
        Assert.That(t.Db.GetOrders(), Has.Count.EqualTo(2));
    }

    [Test, Category("Т.4")]
    [Description("Заказ без заявленных неисправностей не создаётся")]
    public void CreateOrder_RequiresFaults()
    {
        using var t = new TestDatabase();
        var ex = Assert.Catch<BusinessRuleException>(() =>
            t.Admin.CreateOrder(t.Ivanov, t.ActiveModel, new List<Fault>(), "356938035643809"));
        Assert.That(ex!.Field, Is.EqualTo("FaultIds"));
        Assert.That(t.Db.GetOrders(), Is.Empty);
    }

    [Test, Category("Т.6")]
    [Description("Деактивированную модель выбрать нельзя")]
    public void CreateOrder_InactiveModelRejected()
    {
        using var t = new TestDatabase();
        var ex = Assert.Catch<BusinessRuleException>(() =>
            t.Admin.CreateOrder(t.Ivanov, t.InactiveModel, new List<Fault> { t.Fault("Замена дисплея") }, "356938035643809"));
        Assert.That(ex!.Field, Is.EqualTo("PhoneModelId"));
    }

    [Test, Category("Т.4")]
    [Description("IMEI обязателен и состоит из 15 цифр")]
    public void CreateOrder_ImeiValidated()
    {
        using var t = new TestDatabase();
        var ex = Assert.Catch<BusinessRuleException>(() => t.CreateOrder("12345"));
        Assert.That(ex!.Field, Is.EqualTo("Imei"));
    }

    [Test, Category("Т.6")]
    [Description("Повторно выбранная неисправность учитывается один раз")]
    public void CreateOrder_DuplicateFaultsCountedOnce()
    {
        using var t = new TestDatabase();
        var display = t.Fault("Замена дисплея");
        var order = t.Admin.CreateOrder(t.Ivanov, t.ActiveModel, new List<Fault> { display, display }, "356938035643809");
        Assert.That(order.TotalPrice, Is.EqualTo(4500m));
        Assert.That(t.Db.GetOrder(order.Id)!.Faults, Has.Count.EqualTo(1));
    }

    [Test, Category("Т.4"), Category("Т.8")]
    [Description("Изменение basePrice в справочнике не меняет стоимость уже оформленного заказа")]
    public void TotalPrice_FixedAtCreation()
    {
        using var t = new TestDatabase();
        var order = t.CreateOrder();
        var display = t.Fault("Замена дисплея");
        t.Admin.UpdateFault(display.Id, display.Title, 9999m);
        Assert.That(t.Db.GetOrder(order.Id)!.TotalPrice, Is.EqualTo(7000m));
    }

    [Test, Category("Т.8")]
    [Description("AddFault() и UpdateFault() сохраняют изменения справочника")]
    public void Faults_AddAndUpdate()
    {
        using var t = new TestDatabase();
        t.Admin.AddFault("Замена динамика", 1500m);
        var fault = t.Fault("Замена динамика");
        Assert.That(fault.BasePrice, Is.EqualTo(1500m));

        t.Admin.UpdateFault(fault.Id, "Замена разговорного динамика", 1700.5m);
        var updated = t.Db.GetFault(fault.Id);
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Title, Is.EqualTo("Замена разговорного динамика"));
        Assert.That(updated.BasePrice, Is.EqualTo(1700.5m));
    }

    [Test, Category("Т.8")]
    [Description("Альт.: «Такая неисправность уже существует» (без учёта регистра)")]
    public void Faults_DuplicateTitleRejected()
    {
        using var t = new TestDatabase();
        var ex = Assert.Catch<BusinessRuleException>(() => t.Admin.AddFault("замена ДИСПЛЕЯ", 100m));
        Assert.That(ex!.Message, Is.EqualTo("Такая неисправность уже существует"));

        var battery = t.Fault("Замена аккумулятора");
        ex = Assert.Catch<BusinessRuleException>(() => t.Admin.UpdateFault(battery.Id, "Замена дисплея", 100m));
        Assert.That(ex!.Message, Is.EqualTo("Такая неисправность уже существует"));
        Assert.That(t.Db.GetFaults(), Has.Count.EqualTo(5));
    }

    [Test, Category("Т.8")]
    [Description("Наименование обязательно, стоимость не может быть отрицательной")]
    public void Faults_Validation()
    {
        using var t = new TestDatabase();
        Assert.That(Assert.Catch<BusinessRuleException>(() => t.Admin.AddFault("  ", 100m))!.Field, Is.EqualTo("Title"));
        Assert.That(Assert.Catch<BusinessRuleException>(() => t.Admin.AddFault("Чистка", -1m))!.Field, Is.EqualTo("BasePrice"));
    }

    [Test, Category("Т.9")]
    [Description("AddPhone() добавляет активную модель")]
    public void Phones_Add()
    {
        using var t = new TestDatabase();
        t.Admin.AddPhone("Google", "Pixel 8");
        var models = t.Db.GetPhoneModels(activeOnly: true).Where(m => m.Brand == "Google").ToList();
        Assert.That(models, Has.Count.EqualTo(1));
        Assert.That(models[0].ModelName, Is.EqualTo("Pixel 8"));
        Assert.That(models[0].IsActive, Is.True);
    }

    [Test, Category("Т.9")]
    [Description("UpdatePhone() изменяет модель; деактивированная модель не предлагается в заказе")]
    public void Phones_UpdateAndDeactivate()
    {
        using var t = new TestDatabase();
        var model = t.ActiveModel;
        t.Admin.UpdatePhone(model.Id, "Apple", "iPhone 12 mini", true);
        Assert.That(t.Db.GetPhoneModel(model.Id)!.ModelName, Is.EqualTo("iPhone 12 mini"));

        t.Admin.UpdatePhone(model.Id, "Apple", "iPhone 12 mini", false);
        Assert.That(t.Db.GetPhoneModel(model.Id)!.IsActive, Is.False);
        Assert.That(t.Db.GetPhoneModels(activeOnly: true).Select(m => m.Id), Does.Not.Contain(model.Id));
        Assert.That(t.Db.GetPhoneModels().Select(m => m.Id), Does.Contain(model.Id));
    }

    [Category("Т.9")]
    [Description("Альт.: не заполнены бренд или название модели — сохранение блокируется")]
    [TestCase("", "Pixel 8", "Brand")]
    [TestCase("Google", " ", "ModelName")]
    public void Phones_RequiredFields(string brand, string modelName, string field)
    {
        using var t = new TestDatabase();
        int before = t.Db.GetPhoneModels().Count;
        var ex = Assert.Catch<BusinessRuleException>(() => t.Admin.AddPhone(brand, modelName));
        Assert.That(ex!.Field, Is.EqualTo(field));
        Assert.That(t.Db.GetPhoneModels(), Has.Count.EqualTo(before));
    }
}

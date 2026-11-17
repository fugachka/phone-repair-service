using PhoneRepairService.Models;
using PhoneRepairService.Services;

namespace PhoneRepairService.Tests.Unit;

/// <summary>Модульные тесты класса Order: расчёт стоимости, переходы статусов, квитанция.</summary>
[TestFixture]
public class OrderTests
{
    private static Order NewOrder(OrderStatus status = OrderStatus.Created) => new()
    {
        Id = 17,
        Imei = "356938035643809",
        CreationDate = new DateTime(2026, 11, 15, 10, 30, 0),
        Status = status,
        Client = new Client { FullName = "Иванов Иван Иванович", PhoneNumber = "+79001234567" },
        PhoneModel = new PhoneModel { Brand = "Apple", ModelName = "iPhone 12" },
        Faults = new List<Fault>
        {
            new() { Id = 1, Title = "Замена дисплея", BasePrice = 4500m },
            new() { Id = 2, Title = "Замена аккумулятора", BasePrice = 2500.50m }
        }
    };

    [Test, Category("Т.4"), Category("Т.6")]
    [Description("CalculateTotalPrice() суммирует basePrice заявленных неисправностей")]
    public void CalculateTotalPrice_SumsBasePrices()
    {
        var order = NewOrder();
        Assert.That(order.CalculateTotalPrice(), Is.EqualTo(7000.50m));
        Assert.That(order.TotalPrice, Is.EqualTo(7000.50m));
    }

    [Category("Табл. 12")]
    [Description("Допустимый переход статуса")]
    [TestCase(OrderStatus.Created, OrderStatus.InDiagnostics)]
    [TestCase(OrderStatus.Created, OrderStatus.Cancelled)]
    [TestCase(OrderStatus.InDiagnostics, OrderStatus.InRepair)]
    [TestCase(OrderStatus.InDiagnostics, OrderStatus.Cancelled)]
    [TestCase(OrderStatus.InRepair, OrderStatus.Ready)]
    [TestCase(OrderStatus.InRepair, OrderStatus.Cancelled)]
    public void AllowedTransitions(OrderStatus from, OrderStatus to)
    {
        Assert.That(NewOrder(from).CanChangeStatusTo(to), Is.True);
    }

    [Category("Табл. 12")]
    [Description("Недопустимый переход статуса")]
    [TestCase(OrderStatus.Created, OrderStatus.InRepair)]
    [TestCase(OrderStatus.Created, OrderStatus.Ready)]
    [TestCase(OrderStatus.InDiagnostics, OrderStatus.Created)]
    [TestCase(OrderStatus.InDiagnostics, OrderStatus.Ready)]
    [TestCase(OrderStatus.InRepair, OrderStatus.InDiagnostics)]
    [TestCase(OrderStatus.Ready, OrderStatus.Cancelled)]
    [TestCase(OrderStatus.Cancelled, OrderStatus.Created)]
    [TestCase(OrderStatus.Created, OrderStatus.Created)]
    public void ForbiddenTransitions(OrderStatus from, OrderStatus to)
    {
        Assert.That(NewOrder(from).CanChangeStatusTo(to), Is.False);
    }

    [Test, Category("Табл. 12")]
    [Description("Из «Готов к выдаче» и «Отменён» переходов нет")]
    public void FinalStates_HaveNoTransitions()
    {
        Assert.That(NewOrder(OrderStatus.Ready).GetAllowedStatuses(), Is.Empty);
        Assert.That(NewOrder(OrderStatus.Cancelled).GetAllowedStatuses(), Is.Empty);
        Assert.That(OrderStatus.Ready.IsFinal(), Is.True);
        Assert.That(OrderStatus.Cancelled.IsFinal(), Is.True);
        Assert.That(OrderStatus.InRepair.IsFinal(), Is.False);
    }

    [Test, Category("Т.7")]
    [Description("PrintReceipt() содержит номер, ФИО, модель, IMEI, неисправности, стоимость и условия")]
    public void PrintReceipt_ContainsRequiredData()
    {
        var order = NewOrder();
        order.CalculateTotalPrice();
        string text = order.PrintReceipt();
        Assert.That(text, Does.Contain("КВИТАНЦИЯ № 17"));
        Assert.That(text, Does.Contain("Иванов Иван Иванович"));
        Assert.That(text, Does.Contain("Apple iPhone 12"));
        Assert.That(text, Does.Contain("IMEI: 356938035643809"));
        Assert.That(text, Does.Contain("Замена дисплея"));
        Assert.That(text, Does.Contain("Замена аккумулятора"));
        Assert.That(text, Does.Contain("7\u00A0000,50 руб."));
        Assert.That(text, Does.Contain("Условия обслуживания"));
    }

    [Test, Category("Т.11")]
    [Description("GetOrderDetails() показывает модель, статус, диагноз (если указан) и стоимость")]
    public void GetOrderDetails_ShowsDiagnosisWhenPresent()
    {
        var order = NewOrder(OrderStatus.InDiagnostics);
        order.CalculateTotalPrice();
        Assert.That(order.GetOrderDetails(), Does.Contain("Диагноз: не указан"));
        order.Diagnosis = "Повреждён шлейф дисплея";
        string details = order.GetOrderDetails();
        Assert.That(details, Does.Contain("Диагноз: Повреждён шлейф дисплея"));
        Assert.That(details, Does.Contain("Статус: На диагностике"));
        Assert.That(details, Does.Contain("Модель: Apple iPhone 12"));
    }

    [Test]
    [Description("Статусы отображаются русскими названиями из ПЗ")]
    public void StatusDisplayNames()
    {
        Assert.That(OrderStatus.Created.ToDisplayName(), Is.EqualTo("Принят в ремонт"));
        Assert.That(OrderStatus.InDiagnostics.ToDisplayName(), Is.EqualTo("На диагностике"));
        Assert.That(OrderStatus.InRepair.ToDisplayName(), Is.EqualTo("В процессе ремонта"));
        Assert.That(OrderStatus.Ready.ToDisplayName(), Is.EqualTo("Готов к выдаче"));
        Assert.That(OrderStatus.Cancelled.ToDisplayName(), Is.EqualTo("Отменён"));
    }
}

/// <summary>Модульные тесты справочных классов и вспомогательных функций.</summary>
[TestFixture]
public class CatalogAndHelpersTests
{
    [Test]
    [Description("PhoneModel.GetFullName() и Fault.GetFaultDetails()")]
    public void CatalogMethods()
    {
        Assert.That(new PhoneModel { Brand = "Samsung", ModelName = "Galaxy S21" }.GetFullName(),
            Is.EqualTo("Samsung Galaxy S21"));
        Assert.That(new Fault { Title = "Замена дисплея", BasePrice = 4500m }.GetFaultDetails(),
            Is.EqualTo("Замена дисплея — 4\u00A0500,00 руб."));
    }

    [Test, Category("Т.5"), Category("Т.10")]
    [Description("Пароль хранится в виде хэша и проверяется по хэшу")]
    public void PasswordHashing_HashAndVerify()
    {
        string hash1 = PasswordHashing.Hash("secret12");
        string hash2 = PasswordHashing.Hash("secret12");
        Assert.That(hash1, Is.Not.EqualTo("secret12"));
        Assert.That(hash2, Is.Not.EqualTo(hash1));
        Assert.That(PasswordHashing.Verify(hash1, "secret12"), Is.True);
        Assert.That(PasswordHashing.Verify(hash1, "secret13"), Is.False);
        Assert.That(PasswordHashing.Verify("не-хэш", "secret12"), Is.False);
    }

    [Category("Т.5")]
    [Description("Номер телефона приводится к единому виду")]
    [TestCase("+7 (900) 123-45-67", "+79001234567")]
    [TestCase("89001234567", "+79001234567")]
    [TestCase("9001234567", "+79001234567")]
    [TestCase("+7 900 123 45 67", "+79001234567")]
    public void PhoneNormalize(string input, string expected)
    {
        Assert.That(PhoneNumbers.Normalize(input), Is.EqualTo(expected));
    }

    [Category("Т.5")]
    [Description("Некорректный номер телефона отклоняется")]
    [TestCase("")]
    [TestCase("12345")]
    [TestCase("телефон")]
    public void PhoneNormalize_Invalid(string input)
    {
        Assert.That(PhoneNumbers.Normalize(input), Is.Null);
    }

    [Category("Т.4")]
    [Description("Проверка формата IMEI (15 цифр)")]
    [TestCase("356938035643809", true)]
    [TestCase("35693803564380", false)]
    [TestCase("3569380356438090", false)]
    [TestCase("35693803564380A", false)]
    [TestCase("", false)]
    public void ImeiValidation(string imei, bool valid)
    {
        Assert.That(Validation.IsValidImei(imei), Is.EqualTo(valid));
    }
}

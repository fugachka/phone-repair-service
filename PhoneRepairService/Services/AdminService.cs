using PhoneRepairService.Data;
using PhoneRepairService.Models;

namespace PhoneRepairService.Services;

/// <summary>
/// Операции класса Admin диаграммы классов: CreateOrder, RegisterClient, AddPhone, UpdatePhone,
/// AddFault, UpdateFault. Имена и параметры операций совпадают с диаграммой.
/// </summary>
public class AdminService
{
    private readonly RepairDb _db;
    private readonly TimeProvider _time;

    public AdminService(RepairDb db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    /// <summary>Т.4 «Оформить заказ на ремонт»: создаёт заказ в статусе «Принят в ремонт», мастер не назначается.</summary>
    public Order CreateOrder(Client client, PhoneModel model, List<Fault> faults, string imei)
    {
        if (client == null || _db.GetClient(client.Id) == null)
            throw new BusinessRuleException("Выберите клиента.", "ClientId");
        if (model == null || _db.GetPhoneModel(model.Id) is not { } storedModel)
            throw new BusinessRuleException("Выберите модель телефона.", "PhoneModelId");
        if (!storedModel.IsActive)
            throw new BusinessRuleException("Модель деактивирована и не может быть выбрана.", "PhoneModelId");
        var distinctFaults = (faults ?? new List<Fault>()).GroupBy(f => f.Id).Select(g => g.First()).ToList();
        if (distinctFaults.Count == 0)
            throw new BusinessRuleException("Выберите хотя бы одну неисправность.", "FaultIds");
        var storedFaults = distinctFaults.Select(f => _db.GetFault(f.Id)
            ?? throw new BusinessRuleException("Неисправность не найдена в справочнике.", "FaultIds")).ToList();
        imei = (imei ?? "").Trim();
        if (!Validation.IsValidImei(imei))
            throw new BusinessRuleException("IMEI должен состоять из 15 цифр.", "Imei");

        var order = new Order
        {
            Imei = imei,
            CreationDate = Now(),
            Status = OrderStatus.Created,
            StatusChangedDate = null,
            Diagnosis = null,
            ClientId = client.Id,
            Client = client,
            PhoneModelId = storedModel.Id,
            PhoneModel = storedModel,
            MasterId = null,
            Faults = storedFaults
        };
        order.CalculateTotalPrice();
        _db.InTransaction(() => _db.InsertOrder(order));
        return order;
    }

    /// <summary>Т.5 «Зарегистрировать клиента»: проверяет уникальность телефона и логина, хранит хэш пароля.</summary>
    public Client RegisterClient(string fullName, string phone, string email, string login, string password)
    {
        fullName = (fullName ?? "").Trim();
        email = (email ?? "").Trim();
        login = (login ?? "").Trim();
        if (fullName.Length == 0)
            throw new BusinessRuleException("Укажите ФИО клиента.", "FullName");
        string? normalizedPhone = PhoneNumbers.Normalize(phone);
        if (normalizedPhone == null)
            throw new BusinessRuleException("Укажите корректный номер телефона (10–15 цифр).", "Phone");
        if (!Validation.IsValidEmail(email))
            throw new BusinessRuleException("Укажите корректный адрес электронной почты.", "Email");
        if (!Validation.IsValidLogin(login))
            throw new BusinessRuleException("Логин: 3–50 латинских букв, цифр или символов . _ -", "Login");
        if (string.IsNullOrEmpty(password) || password.Length < 6)
            throw new BusinessRuleException("Пароль должен содержать не менее 6 символов.", "Password");

        if (_db.FindClientByPhone(normalizedPhone) is { } existing)
            throw new DuplicateClientPhoneException(existing);
        if (_db.LoginExists(login))
            throw new DuplicateLoginException(login);

        var client = new Client
        {
            FullName = fullName,
            PhoneNumber = normalizedPhone,
            Email = email,
            Login = login,
            PasswordHash = PasswordHashing.Hash(password)
        };
        _db.InsertUser(client);
        return client;
    }

    /// <summary>Т.9: добавляет модель телефона в справочник.</summary>
    public void AddPhone(string brand, string modelName)
    {
        (brand, modelName) = ValidatePhone(brand, modelName);
        _db.InsertPhoneModel(new PhoneModel { Brand = brand, ModelName = modelName, IsActive = true });
    }

    /// <summary>Т.9: изменяет бренд и название модели или деактивирует её (isActive = false).</summary>
    public void UpdatePhone(int modelId, string brand, string modelName, bool isActive)
    {
        var model = _db.GetPhoneModel(modelId) ?? throw new BusinessRuleException("Модель не найдена.");
        (brand, modelName) = ValidatePhone(brand, modelName);
        model.Brand = brand;
        model.ModelName = modelName;
        model.IsActive = isActive;
        _db.UpdatePhoneModel(model);
    }

    /// <summary>Т.8: добавляет неисправность в справочник.</summary>
    public void AddFault(string title, decimal basePrice)
    {
        title = ValidateFault(title, basePrice, exceptId: null);
        _db.InsertFault(new Fault { Title = title, BasePrice = basePrice });
    }

    /// <summary>Т.8: изменяет наименование и/или базовую стоимость неисправности.</summary>
    public void UpdateFault(int faultId, string title, decimal basePrice)
    {
        var fault = _db.GetFault(faultId) ?? throw new BusinessRuleException("Неисправность не найдена.");
        fault.Title = ValidateFault(title, basePrice, exceptId: faultId);
        fault.BasePrice = basePrice;
        _db.UpdateFault(fault);
    }

    private static (string brand, string modelName) ValidatePhone(string brand, string modelName)
    {
        brand = (brand ?? "").Trim();
        modelName = (modelName ?? "").Trim();
        if (brand.Length == 0)
            throw new BusinessRuleException("Укажите бренд.", "Brand");
        if (modelName.Length == 0)
            throw new BusinessRuleException("Укажите название модели.", "ModelName");
        return (brand, modelName);
    }

    private string ValidateFault(string title, decimal basePrice, int? exceptId)
    {
        title = string.Join(' ', (title ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (title.Length == 0)
            throw new BusinessRuleException("Укажите наименование неисправности.", "Title");
        if (basePrice < 0)
            throw new BusinessRuleException("Базовая стоимость не может быть отрицательной.", "BasePrice");
        bool duplicate = _db.GetFaults().Any(f => f.Id != exceptId &&
            string.Equals(f.Title, title, StringComparison.CurrentCultureIgnoreCase));
        if (duplicate)
            throw new BusinessRuleException("Такая неисправность уже существует", "Title");
        return title;
    }

    private DateTime Now() => TrimToSeconds(_time.GetLocalNow().DateTime);

    internal static DateTime TrimToSeconds(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Kind);
}

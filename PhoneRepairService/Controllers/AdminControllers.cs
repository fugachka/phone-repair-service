using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneRepairService.Data;
using PhoneRepairService.Models;
using PhoneRepairService.Services;
using PhoneRepairService.ViewModels;

namespace PhoneRepairService.Controllers;

/// <summary>Т.4 «Оформить заказ на ремонт», Т.6 «Выбрать модель и неисправность», Т.7 «Распечатать квитанцию».</summary>
[Authorize(Roles = Roles.Admin)]
public class OrdersController : AppController
{
    private readonly RepairDb _db;
    private readonly AdminService _admin;

    public OrdersController(RepairDb db, AdminService admin)
    {
        _db = db;
        _admin = admin;
    }

    /// <summary>Список оформленных заказов (вспомогательная страница для повторной печати квитанции).</summary>
    public IActionResult Index() => View(_db.GetOrders());

    /// <summary>Т.4, шаг 1: «Создать новый заказ».</summary>
    [HttpGet]
    public IActionResult Create(int? clientId) =>
        View(Fill(new OrderCreateViewModel { ClientId = clientId }));

    /// <summary>Т.4, шаги 4–6: ввести IMEI и «Сохранить заказ» (CreateOrder), затем Т.7.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(OrderCreateViewModel model)
    {
        try
        {
            var client = model.ClientId is int cid ? _db.GetClient(cid) : null;
            var phoneModel = model.PhoneModelId is int mid ? _db.GetPhoneModel(mid) : null;
            var faults = model.FaultIds.Distinct().Select(id => _db.GetFault(id)).OfType<Fault>().ToList();
            var order = _admin.CreateOrder(client!, phoneModel!, faults, model.Imei ?? "");
            Flash($"Заказ № {order.Id} сохранён, статус «{order.Status.ToDisplayName()}».");
            return RedirectToAction(nameof(Receipt), new { id = order.Id });
        }
        catch (BusinessRuleException ex)
        {
            AddError(ex);
            return View(Fill(model));
        }
    }

    /// <summary>Т.7: печатная форма квитанции (PrintReceipt).</summary>
    public IActionResult Receipt(int id)
    {
        var order = _db.GetOrder(id);
        return order == null ? NotFound() : View(order);
    }

    private OrderCreateViewModel Fill(OrderCreateViewModel model)
    {
        model.Clients = _db.GetClients();
        model.PhoneModels = _db.GetPhoneModels(activeOnly: true);
        model.Faults = _db.GetFaults();
        return model;
    }
}

/// <summary>Т.5 «Зарегистрировать клиента».</summary>
[Authorize(Roles = Roles.Admin)]
public class ClientsController : AppController
{
    private readonly RepairDb _db;
    private readonly AdminService _admin;

    public ClientsController(RepairDb db, AdminService admin)
    {
        _db = db;
        _admin = admin;
    }

    public IActionResult Index() => View(_db.GetClients());

    [HttpGet]
    public IActionResult Register(bool fromOrder = false) =>
        View(new ClientRegisterViewModel { FromOrder = fromOrder });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Register(ClientRegisterViewModel model)
    {
        try
        {
            var client = _admin.RegisterClient(model.FullName ?? "", model.Phone ?? "", model.Email ?? "",
                model.Login ?? "", model.Password ?? "");
            Flash($"Клиент «{client.FullName}» зарегистрирован, учётная запись «{client.Login}» создана.");
            return model.FromOrder
                ? RedirectToAction("Create", "Orders", new { clientId = client.Id })
                : RedirectToAction(nameof(Index));
        }
        catch (DuplicateClientPhoneException ex)
        {
            model.ExistingClient = ex.ExistingClient;
            return View(model);
        }
        catch (BusinessRuleException ex)
        {
            AddError(ex);
            return View(model);
        }
    }
}

/// <summary>Т.8 «Вести справочник неисправностей».</summary>
[Authorize(Roles = Roles.Admin)]
public class FaultsController : AppController
{
    private readonly RepairDb _db;
    private readonly AdminService _admin;

    public FaultsController(RepairDb db, AdminService admin)
    {
        _db = db;
        _admin = admin;
    }

    public IActionResult Index() => View(_db.GetFaults());

    /// <summary>id = 0 — добавление новой неисправности, иначе изменение существующей.</summary>
    [HttpGet]
    public IActionResult Edit(int id = 0, string? returnUrl = null)
    {
        if (id == 0) return View(new FaultEditViewModel { ReturnUrl = returnUrl });
        var fault = _db.GetFault(id);
        if (fault == null) return NotFound();
        return View(new FaultEditViewModel
        {
            Id = fault.Id,
            Title = fault.Title,
            BasePrice = fault.BasePrice.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            ReturnUrl = returnUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(FaultEditViewModel model)
    {
        try
        {
            if (!FaultEditViewModel.TryParsePrice(model.BasePrice, out decimal price))
                throw new BusinessRuleException("Укажите базовую стоимость числом, например 2500 или 2500,50.", "BasePrice");
            if (model.Id == 0)
                _admin.AddFault(model.Title ?? "", price);
            else
                _admin.UpdateFault(model.Id, model.Title ?? "", price);
            Flash("Справочник неисправностей сохранён.");
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleException ex)
        {
            AddError(ex);
            return View(model);
        }
    }
}

/// <summary>Т.9 «Вести справочник моделей телефонов».</summary>
[Authorize(Roles = Roles.Admin)]
public class PhoneModelsController : AppController
{
    private readonly RepairDb _db;
    private readonly AdminService _admin;

    public PhoneModelsController(RepairDb db, AdminService admin)
    {
        _db = db;
        _admin = admin;
    }

    public IActionResult Index() => View(_db.GetPhoneModels());

    [HttpGet]
    public IActionResult Edit(int id = 0, string? returnUrl = null)
    {
        if (id == 0) return View(new PhoneModelEditViewModel { ReturnUrl = returnUrl });
        var model = _db.GetPhoneModel(id);
        if (model == null) return NotFound();
        return View(new PhoneModelEditViewModel
        {
            Id = model.Id,
            Brand = model.Brand,
            ModelName = model.ModelName,
            IsActive = model.IsActive,
            ReturnUrl = returnUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Edit(PhoneModelEditViewModel model)
    {
        try
        {
            if (model.Id == 0)
                _admin.AddPhone(model.Brand ?? "", model.ModelName ?? "");
            else
                _admin.UpdatePhone(model.Id, model.Brand ?? "", model.ModelName ?? "", model.IsActive);
            Flash("Каталог моделей телефонов обновлён.");
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleException ex)
        {
            AddError(ex);
            return View(model);
        }
    }

    /// <summary>Деактивация устаревшей модели (UpdatePhone с isActive = false).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Deactivate(int id)
    {
        var model = _db.GetPhoneModel(id);
        if (model == null) return NotFound();
        _admin.UpdatePhone(model.Id, model.Brand, model.ModelName, false);
        Flash($"Модель «{model.GetFullName()}» деактивирована.");
        return RedirectToAction(nameof(Index));
    }
}

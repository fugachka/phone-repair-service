using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneRepairService.Data;
using PhoneRepairService.Models;
using PhoneRepairService.Services;
using PhoneRepairService.ViewModels;

namespace PhoneRepairService.Controllers;

/// <summary>Личный кабинет владельца телефона — Т.1 «Просмотреть свои заказы».</summary>
[Authorize(Roles = Roles.Client)]
public class MyOrdersController : AppController
{
    private readonly RepairDb _db;
    private readonly ClientService _clients;

    public MyOrdersController(RepairDb db, ClientService clients)
    {
        _db = db;
        _clients = clients;
    }

    public IActionResult Index()
    {
        var client = CurrentUser<Client>(_db);
        if (client == null) return Forbid();
        return View(_clients.ViewMyOrders(client));
    }

    public IActionResult Details(int id)
    {
        var client = CurrentUser<Client>(_db);
        if (client == null) return Forbid();
        var order = _clients.GetMyOrder(client, id);
        return order == null ? NotFound() : View(order);
    }
}

/// <summary>Рабочее место мастера — Т.2 «Указать диагноз», Т.3 «Изменить статус заказа».</summary>
[Authorize(Roles = Roles.Master)]
public class WorkOrdersController : AppController
{
    private readonly RepairDb _db;
    private readonly MasterService _masters;

    public WorkOrdersController(RepairDb db, MasterService masters)
    {
        _db = db;
        _masters = masters;
    }

    /// <summary>Общий список заказов.</summary>
    public IActionResult Index() => View(_db.GetOrders());

    /// <summary>Карточка заказа: диагноз и допустимые значения статуса.</summary>
    public IActionResult Details(int id)
    {
        var order = _db.GetOrder(id);
        if (order == null) return NotFound();
        return View(new WorkOrderViewModel
        {
            Order = order,
            DiagnosisText = order.Diagnosis,
            AllowedStatuses = order.GetAllowedStatuses()
        });
    }

    /// <summary>Т.2, шаги 3–5: сохранить диагноз (SetDiagnosis). Статус не меняется.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveDiagnosis(int id, string? diagnosisText)
    {
        var master = CurrentUser<Master>(_db);
        if (master == null) return Forbid();
        try
        {
            _masters.SetDiagnosis(master, id, diagnosisText ?? "");
            Flash("Диагноз сохранён. Статус заказа не изменился.");
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleException ex)
        {
            var order = _db.GetOrder(id);
            if (order == null) return NotFound();
            AddError(ex);
            return View(nameof(Details), new WorkOrderViewModel
            {
                Order = order,
                DiagnosisText = diagnosisText,
                AllowedStatuses = order.GetAllowedStatuses()
            });
        }
    }

    /// <summary>Т.3, шаг 3: мастер выбрал новое значение статуса — переход к подтверждению.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SelectStatus(int id, OrderStatus? newStatus)
    {
        var order = _db.GetOrder(id);
        if (order == null) return NotFound();
        if (newStatus is not { } status || !order.CanChangeStatusTo(status))
        {
            Flash("Выберите допустимое значение статуса.");
            return RedirectToAction(nameof(Details), new { id });
        }
        return RedirectToAction(nameof(ConfirmStatus), new { id, newStatus = status, step = 1 });
    }

    /// <summary>Т.3, шаг 4 и альтернативный поток: подтверждение (для конечного статуса — повторное).</summary>
    [HttpGet]
    public IActionResult ConfirmStatus(int id, OrderStatus newStatus, int step = 1)
    {
        var order = _db.GetOrder(id);
        if (order == null) return NotFound();
        if (!order.CanChangeStatusTo(newStatus))
        {
            Flash("Выберите допустимое значение статуса.");
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(new StatusConfirmViewModel { Order = order, NewStatus = newStatus, Step = step == 2 ? 2 : 1 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [ActionName("ConfirmStatus")]
    public IActionResult ConfirmStatusPost(int id, OrderStatus newStatus, int step, string decision)
    {
        if (decision != "confirm")
        {
            Flash("Действие отменено, статус заказа не изменён.");
            return RedirectToAction(nameof(Details), new { id });
        }
        if (newStatus.IsFinal() && step < 2)
            return RedirectToAction(nameof(ConfirmStatus), new { id, newStatus, step = 2 });

        var master = CurrentUser<Master>(_db);
        if (master == null) return Forbid();
        try
        {
            _masters.UpdateOrderStatus(master, id, newStatus);
            Flash($"Статус заказа изменён на «{newStatus.ToDisplayName()}».");
        }
        catch (BusinessRuleException ex)
        {
            Flash(ex.Message);
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}

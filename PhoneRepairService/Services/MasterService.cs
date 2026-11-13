using PhoneRepairService.Data;
using PhoneRepairService.Models;

namespace PhoneRepairService.Services;

/// <summary>
/// Операции класса Master диаграммы классов: SetDiagnosis(orderId, diagnosisText) и
/// UpdateOrderStatus(orderId, status). Первый параметр — мастер, от имени которого выполняется операция.
/// </summary>
public class MasterService
{
    private readonly RepairDb _db;
    private readonly TimeProvider _time;

    public MasterService(RepairDb db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    /// <summary>Т.2 «Указать диагноз»: записывает диагноз. Статус и стоимость заказа не меняются.</summary>
    public void SetDiagnosis(Master master, int orderId, string diagnosisText)
    {
        EnsureMaster(master);
        var order = _db.GetOrder(orderId) ?? throw new BusinessRuleException("Заказ не найден.");
        if (order.Status != OrderStatus.InDiagnostics)
            throw new BusinessRuleException("Диагноз указывается для заказа в статусе «На диагностике».");
        diagnosisText = (diagnosisText ?? "").Trim();
        if (diagnosisText.Length == 0)
            throw new BusinessRuleException("Заполните поле «Диагноз».", "DiagnosisText");

        order.Diagnosis = diagnosisText;
        _db.UpdateOrderState(order);
    }

    /// <summary>
    /// Т.3 «Изменить статус заказа»: только допустимые переходы (табл. 12 ПЗ); сохраняет дату и время изменения;
    /// при переходе «Принят в ремонт» → «На диагностике» связывает заказ с мастером.
    /// </summary>
    public void UpdateOrderStatus(Master master, int orderId, OrderStatus status)
    {
        EnsureMaster(master);
        var order = _db.GetOrder(orderId) ?? throw new BusinessRuleException("Заказ не найден.");
        if (order.Status.IsFinal())
            throw new BusinessRuleException(
                $"Статус «{order.Status.ToDisplayName()}» конечный, изменить его нельзя.");
        if (!order.CanChangeStatusTo(status))
            throw new BusinessRuleException(
                $"Переход из «{order.Status.ToDisplayName()}» в «{status.ToDisplayName()}» недопустим.");

        if (order.Status == OrderStatus.Created && status == OrderStatus.InDiagnostics)
        {
            order.MasterId = master.Id;
            order.Master = master;
        }
        order.Status = status;
        order.StatusChangedDate = AdminService.TrimToSeconds(_time.GetLocalNow().DateTime);
        _db.UpdateOrderState(order);
    }

    private void EnsureMaster(Master master)
    {
        if (master == null || _db.GetMaster(master.Id) == null)
            throw new BusinessRuleException("Операция доступна только мастеру.");
    }
}

/// <summary>Операция класса Client диаграммы классов: ViewMyOrders().</summary>
public class ClientService
{
    private readonly RepairDb _db;

    public ClientService(RepairDb db)
    {
        _db = db;
    }

    /// <summary>Т.1 «Просмотреть свои заказы»: заказы клиента с текущими статусами.</summary>
    public List<Order> ViewMyOrders(Client client) => _db.GetOrdersByClient(client.Id);

    /// <summary>Карточка заказа; null, если заказ не принадлежит клиенту.</summary>
    public Order? GetMyOrder(Client client, int orderId)
    {
        var order = _db.GetOrder(orderId);
        return order != null && order.ClientId == client.Id ? order : null;
    }
}

/// <summary>Т.10 «Авторизоваться»: поиск учётной записи по логину и проверка пароля по хэшу.</summary>
public class AuthService
{
    private readonly RepairDb _db;

    public AuthService(RepairDb db)
    {
        _db = db;
    }

    public User? Authenticate(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password)) return null;
        var user = _db.FindUserByLogin(login.Trim());
        return user != null && PasswordHashing.Verify(user.PasswordHash, password) ? user : null;
    }
}

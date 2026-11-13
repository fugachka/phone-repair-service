using System.Globalization;
using PhoneRepairService.Models;

namespace PhoneRepairService.ViewModels;

public class LoginViewModel
{
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? ReturnUrl { get; set; }
}

/// <summary>Форма оформления заказа (Т.4) с выбором модели и неисправностей (Т.6).</summary>
public class OrderCreateViewModel
{
    public int? ClientId { get; set; }
    public int? PhoneModelId { get; set; }
    public List<int> FaultIds { get; set; } = new();
    public string? Imei { get; set; }

    public List<Client> Clients { get; set; } = new();
    public List<PhoneModel> PhoneModels { get; set; } = new();
    public List<Fault> Faults { get; set; } = new();

    public decimal SelectedTotal => Faults.Where(f => FaultIds.Contains(f.Id)).Sum(f => f.BasePrice);
}

/// <summary>Форма регистрации клиента (Т.5).</summary>
public class ClientRegisterViewModel
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    /// <summary>true — регистрация вызвана из оформления заказа (расширение Т.4).</summary>
    public bool FromOrder { get; set; }

    public Client? ExistingClient { get; set; }
}

public class FaultEditViewModel
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? BasePrice { get; set; }
    public string? ReturnUrl { get; set; }

    public static bool TryParsePrice(string? text, out decimal price)
    {
        price = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        string normalized = text.Trim().Replace(" ", "").Replace(' '.ToString(), "").Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
    }
}

public class PhoneModelEditViewModel
{
    public int Id { get; set; }
    public string? Brand { get; set; }
    public string? ModelName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ReturnUrl { get; set; }
}

/// <summary>Карточка заказа для мастера (Т.2, Т.3).</summary>
public class WorkOrderViewModel
{
    public Order Order { get; set; } = null!;
    public string? DiagnosisText { get; set; }
    public IReadOnlyList<OrderStatus> AllowedStatuses { get; set; } = Array.Empty<OrderStatus>();
}

/// <summary>Подтверждение изменения статуса (Т.3, шаг 4 и альтернативный поток).</summary>
public class StatusConfirmViewModel
{
    public Order Order { get; set; } = null!;
    public OrderStatus NewStatus { get; set; }
    /// <summary>1 — первое подтверждение, 2 — повторное (для конечных статусов).</summary>
    public int Step { get; set; } = 1;
}

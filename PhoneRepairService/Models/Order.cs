using System.Text;

namespace PhoneRepairService.Models;

/// <summary>Заказ на ремонт — центральная сущность (класс Order).</summary>
public class Order
{
    /// <summary>
    /// Допустимые изменения статуса — таблица 12 ПЗ (диаграмма состояний заказа).
    /// Из конечных состояний Ready и Cancelled переходов нет.
    /// </summary>
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> Transitions =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Created] = new[] { OrderStatus.InDiagnostics, OrderStatus.Cancelled },
            [OrderStatus.InDiagnostics] = new[] { OrderStatus.InRepair, OrderStatus.Cancelled },
            [OrderStatus.InRepair] = new[] { OrderStatus.Ready, OrderStatus.Cancelled },
            [OrderStatus.Ready] = Array.Empty<OrderStatus>(),
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
        };

    /// <summary>Номер заказа.</summary>
    public int Id { get; set; }
    public string Imei { get; set; } = "";
    public DateTime CreationDate { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    /// <summary>Дата и время последнего изменения статуса; до первого изменения не задана.</summary>
    public DateTime? StatusChangedDate { get; set; }
    /// <summary>Диагноз мастера (в том числе заключение о неремонтопригодности).</summary>
    public string? Diagnosis { get; set; }
    /// <summary>Предварительная стоимость ремонта по базовым ценам заявленных неисправностей.</summary>
    public decimal TotalPrice { get; set; }

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public int PhoneModelId { get; set; }
    public PhoneModel PhoneModel { get; set; } = null!;
    /// <summary>Мастер (0..1); связывается при переходе «Принят в ремонт» → «На диагностике».</summary>
    public int? MasterId { get; set; }
    public Master? Master { get; set; }
    /// <summary>Заявленные неисправности (1..*).</summary>
    public List<Fault> Faults { get; set; } = new();

    /// <summary>Рассчитывает предварительную стоимость: сумма basePrice заявленных неисправностей.</summary>
    public decimal CalculateTotalPrice()
    {
        TotalPrice = Faults.Sum(f => f.BasePrice);
        return TotalPrice;
    }

    /// <summary>Текст печатной формы квитанции о приёме устройства в ремонт.</summary>
    public string PrintReceipt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("СЕРВИС РЕМОНТА ТЕЛЕФОНОВ");
        sb.AppendLine($"КВИТАНЦИЯ № {Id} о приёме устройства в ремонт");
        sb.AppendLine($"Дата приёма: {CreationDate:dd.MM.yyyy HH:mm}");
        sb.AppendLine();
        sb.AppendLine($"Клиент: {Client?.FullName}");
        sb.AppendLine($"Телефон клиента: {Client?.PhoneNumber}");
        sb.AppendLine($"Модель: {PhoneModel?.GetFullName()}");
        sb.AppendLine($"IMEI: {Imei}");
        sb.AppendLine("Заявленные неисправности:");
        foreach (var fault in Faults)
            sb.AppendLine($"  - {fault.GetFaultDetails()}");
        sb.AppendLine($"Предварительная стоимость ремонта: {Money.Format(TotalPrice)}");
        sb.AppendLine();
        sb.AppendLine("Условия обслуживания:");
        sb.AppendLine("  1. Окончательная стоимость определяется после диагностики и согласовывается с клиентом.");
        sb.AppendLine("  2. Статус заказа можно отслеживать в личном кабинете.");
        sb.AppendLine("  3. Устройство выдаётся при предъявлении квитанции.");
        sb.AppendLine();
        sb.AppendLine("Подпись клиента: ____________     Подпись администратора: ____________");
        return sb.ToString();
    }

    /// <summary>Сведения о заказе для карточки: модель, статус, диагноз (если указан), стоимость.</summary>
    public string GetOrderDetails()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Заказ № {Id} от {CreationDate:dd.MM.yyyy}");
        sb.AppendLine($"Модель: {PhoneModel?.GetFullName()}");
        sb.AppendLine($"IMEI: {Imei}");
        sb.AppendLine($"Статус: {Status.ToDisplayName()}");
        sb.AppendLine($"Диагноз: {(string.IsNullOrWhiteSpace(Diagnosis) ? "не указан" : Diagnosis)}");
        sb.AppendLine($"Предварительная стоимость: {Money.Format(TotalPrice)}");
        return sb.ToString();
    }

    /// <summary>Значения статуса, в которые можно перевести заказ из текущего состояния.</summary>
    public IReadOnlyList<OrderStatus> GetAllowedStatuses() => Transitions[Status];

    public bool CanChangeStatusTo(OrderStatus newStatus) => Transitions[Status].Contains(newStatus);
}

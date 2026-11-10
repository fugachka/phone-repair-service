namespace PhoneRepairService.Models;

/// <summary>
/// Статус заказа (перечисление OrderStatus диаграммы классов, состояния диаграммы состояний).
/// </summary>
public enum OrderStatus
{
    /// <summary>«Принят в ремонт»</summary>
    Created = 0,
    /// <summary>«На диагностике»</summary>
    InDiagnostics = 1,
    /// <summary>«В процессе ремонта»</summary>
    InRepair = 2,
    /// <summary>«Готов к выдаче» (конечный)</summary>
    Ready = 3,
    /// <summary>«Отменён» (конечный)</summary>
    Cancelled = 4
}

public static class OrderStatusExtensions
{
    /// <summary>Название статуса для отображения пользователю.</summary>
    public static string ToDisplayName(this OrderStatus status) => status switch
    {
        OrderStatus.Created => "Принят в ремонт",
        OrderStatus.InDiagnostics => "На диагностике",
        OrderStatus.InRepair => "В процессе ремонта",
        OrderStatus.Ready => "Готов к выдаче",
        OrderStatus.Cancelled => "Отменён",
        _ => status.ToString()
    };

    /// <summary>Конечные состояния: «Готов к выдаче» и «Отменён».</summary>
    public static bool IsFinal(this OrderStatus status) =>
        status is OrderStatus.Ready or OrderStatus.Cancelled;
}

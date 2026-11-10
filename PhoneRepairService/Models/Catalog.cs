using System.Globalization;

namespace PhoneRepairService.Models;

/// <summary>Модель телефона — элемент справочника моделей (класс PhoneModel).</summary>
public class PhoneModel
{
    public int Id { get; set; }
    public string Brand { get; set; } = "";
    public string ModelName { get; set; } = "";
    /// <summary>false — модель деактивирована и не предлагается при оформлении заказа.</summary>
    public bool IsActive { get; set; } = true;

    public string GetFullName() => $"{Brand} {ModelName}".Trim();
}

/// <summary>Неисправность — элемент справочника неисправностей (класс Fault).</summary>
public class Fault
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    /// <summary>Базовая стоимость устранения, руб.</summary>
    public decimal BasePrice { get; set; }

    public string GetFaultDetails() =>
        $"{Title} — {Money.Format(BasePrice)}";
}

public static class Money
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public static string Format(decimal value) => value.ToString("N2", Ru) + " руб.";
}

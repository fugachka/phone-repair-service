namespace PhoneRepairService.Models;

/// <summary>Роли пользователей (значения столбца Role таблицы Users).</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Master = "Master";
    public const string Client = "Client";

    public static string ToDisplayName(string role) => role switch
    {
        Admin => "Администратор",
        Master => "Мастер",
        Client => "Владелец телефона",
        _ => role
    };
}

/// <summary>Пользователь — базовая учётная запись (класс User диаграммы классов).</summary>
public abstract class User
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";

    /// <summary>Роль, соответствующая классу-наследнику.</summary>
    public abstract string Role { get; }
}

/// <summary>Администратор. Операции класса реализованы в <see cref="Services.AdminService"/>.</summary>
public class Admin : User
{
    public override string Role => Roles.Admin;
}

/// <summary>Мастер. Операции класса реализованы в <see cref="Services.MasterService"/>.</summary>
public class Master : User
{
    public string Specialization { get; set; } = "";
    public override string Role => Roles.Master;
}

/// <summary>Владелец телефона (клиент). Операция ViewMyOrders() — в <see cref="Services.ClientService"/>.</summary>
public class Client : User
{
    public string Email { get; set; } = "";
    public override string Role => Roles.Client;
}

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using PhoneRepairService.Models;

namespace PhoneRepairService.Services;

/// <summary>Нарушение бизнес-правила; текст сообщения показывается пользователю.</summary>
public class BusinessRuleException : Exception
{
    /// <summary>Поле формы, к которому относится ошибка (может отсутствовать).</summary>
    public string? Field { get; }

    public BusinessRuleException(string message, string? field = null) : base(message)
    {
        Field = field;
    }
}

/// <summary>Т.5, альтернативный поток: клиент с таким номером телефона уже зарегистрирован.</summary>
public class DuplicateClientPhoneException : BusinessRuleException
{
    public Client ExistingClient { get; }

    public DuplicateClientPhoneException(Client existing)
        : base("Клиент с таким номером уже зарегистрирован", "Phone")
    {
        ExistingClient = existing;
    }
}

/// <summary>Т.5, альтернативный поток: логин уже занят.</summary>
public class DuplicateLoginException : BusinessRuleException
{
    public DuplicateLoginException(string login)
        : base($"Логин «{login}» уже занят. Введите другой логин.", "Login")
    {
    }
}

/// <summary>Хэширование паролей (в БД хранится только хэш — атрибут passwordHash).</summary>
public static class PasswordHashing
{
    private static readonly PasswordHasher<User> Hasher = new();

    public static string Hash(string password) => Hasher.HashPassword(null!, password);

    public static bool Verify(string passwordHash, string password)
    {
        if (string.IsNullOrEmpty(passwordHash) || password == null) return false;
        try
        {
            return Hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

/// <summary>Приведение номера телефона к единому виду для проверки уникальности.</summary>
public static class PhoneNumbers
{
    /// <summary>
    /// Оставляет только цифры; российский номер 8XXXXXXXXXX приводится к +7XXXXXXXXXX.
    /// Возвращает null, если номер некорректен (меньше 10 или больше 15 цифр).
    /// </summary>
    public static string? Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        string digits = new(phone.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 11 && digits[0] == '8') digits = "7" + digits[1..];
        if (digits.Length == 10) digits = "7" + digits;
        if (digits.Length < 11 || digits.Length > 15) return null;
        return "+" + digits;
    }
}

public static partial class Validation
{
    /// <summary>IMEI — 15 цифр.</summary>
    public static bool IsValidImei(string? imei) => imei != null && ImeiRegex().IsMatch(imei);

    public static bool IsValidLogin(string? login) => login != null && LoginRegex().IsMatch(login);

    public static bool IsValidEmail(string? email) => email != null && EmailRegex().IsMatch(email);

    [GeneratedRegex(@"^\d{15}$")]
    private static partial Regex ImeiRegex();

    [GeneratedRegex(@"^[A-Za-z0-9._-]{3,50}$")]
    private static partial Regex LoginRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}

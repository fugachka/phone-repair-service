using MySql.Data.MySqlClient;

namespace PhoneRepairService.Tests.Services;

/// <summary>Перед тестами с БД проверяет, что сервер MySQL доступен.</summary>
[SetUpFixture]
public class MySqlCheck
{
    [OneTimeSetUp]
    public void CheckConnection()
    {
        try
        {
            using var connection = new MySqlConnection(TestDatabase.ServerConnectionString);
            connection.Open();
        }
        catch (Exception ex)
        {
            Assert.Fail("Нет подключения к MySQL для тестов с БД. Запустите сервер MySQL и создайте пользователя " +
                        "скриптом database/create_user.sql (или задайте строку подключения в переменной окружения " +
                        $"PHONE_REPAIR_TEST_DB). Ошибка: {ex.Message}");
        }
    }
}

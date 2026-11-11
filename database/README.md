# База данных

СУБД — MySQL 8.0 или новее, доступ через Entity Framework Core (провайдер `MySql.EntityFrameworkCore`).

* `create_user.sql` — создание пользователя `phone_repair` (выполняется один раз под root).
* Структура таблиц задаётся классом `PhoneRepairService/Data/RepairDbContext.cs`, база создаётся и обновляется
  миграциями из папки `PhoneRepairService/Migrations` (при запуске приложения выполняется `Database.Migrate()`).
* Начальные данные (сотрудники, демонстрационный клиент, справочники) добавляет `Data/DbInitializer.cs`.

Таблицы: `Users` (User и наследники Admin, Master, Client, столбец `Role`), `PhoneModels`, `Faults`, `Orders`,
`OrderFaults` (связь «многие ко многим» заказа и неисправностей), `__EFMigrationsHistory` (служебная, миграции).

SQL-скрипт всех миграций: `dotnet ef migrations script --project PhoneRepairService -o database/schema.sql`.

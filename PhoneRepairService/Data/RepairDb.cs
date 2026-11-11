using Microsoft.EntityFrameworkCore;
using PhoneRepairService.Models;

namespace PhoneRepairService.Data;

/// <summary>
/// Слой доступа к данным на Entity Framework Core: чтение и сохранение объектов предметной области.
/// Бизнес-правила проверяются в классах сервисов.
/// </summary>
public sealed class RepairDb
{
    public RepairDbContext Context { get; }

    public RepairDb(RepairDbContext context)
    {
        Context = context;
    }

    /// <summary>Выполняет действия в одной транзакции (откат при исключении).</summary>
    public T InTransaction<T>(Func<T> action)
    {
        using var transaction = Context.Database.BeginTransaction();
        T result = action();
        transaction.Commit();
        return result;
    }

    /// <summary>Поиск по логину без учёта регистра.</summary>
    public User? FindUserByLogin(string login)
    {
        string key = login.ToLowerInvariant();
        return Context.Users.FirstOrDefault(u => u.Login.ToLower() == key);
    }

    public User? GetUser(int id) => Context.Users.Find(id);

    public Client? GetClient(int id) => GetUser(id) as Client;

    public Master? GetMaster(int id) => GetUser(id) as Master;

    public List<Client> GetClients() =>
        Context.Users.OfType<Client>().OrderBy(c => c.FullName).ToList();

    public Client? FindClientByPhone(string normalizedPhone) =>
        Context.Users.OfType<Client>().FirstOrDefault(c => c.PhoneNumber == normalizedPhone);

    public bool LoginExists(string login)
    {
        string key = login.ToLowerInvariant();
        return Context.Users.Any(u => u.Login.ToLower() == key);
    }

    public int InsertUser(User user)
    {
        Context.Users.Add(user);
        Context.SaveChanges();
        return user.Id;
    }

    public List<PhoneModel> GetPhoneModels(bool activeOnly = false) =>
        Context.PhoneModels
            .Where(m => !activeOnly || m.IsActive)
            .OrderBy(m => m.Brand).ThenBy(m => m.ModelName)
            .ToList();

    public PhoneModel? GetPhoneModel(int id) => Context.PhoneModels.Find(id);

    public int InsertPhoneModel(PhoneModel model)
    {
        Context.PhoneModels.Add(model);
        Context.SaveChanges();
        return model.Id;
    }

    public void UpdatePhoneModel(PhoneModel model)
    {
        Context.PhoneModels.Update(model);
        Context.SaveChanges();
    }

    public List<Fault> GetFaults() => Context.Faults.OrderBy(f => f.Title).ToList();

    public Fault? GetFault(int id) => Context.Faults.Find(id);

    public int InsertFault(Fault fault)
    {
        Context.Faults.Add(fault);
        Context.SaveChanges();
        return fault.Id;
    }

    public void UpdateFault(Fault fault)
    {
        Context.Faults.Update(fault);
        Context.SaveChanges();
    }

    /// <summary>Заказы вместе с клиентом, моделью, мастером и неисправностями.</summary>
    private IQueryable<Order> OrdersWithDetails() =>
        Context.Orders
            .Include(o => o.Client)
            .Include(o => o.PhoneModel)
            .Include(o => o.Master)
            .Include(o => o.Faults.OrderBy(f => f.Title));

    /// <summary>Сохраняет новый заказ вместе со связями с неисправностями (таблица OrderFaults).</summary>
    public int InsertOrder(Order order)
    {
        Context.Orders.Add(order);
        Context.SaveChanges();
        return order.Id;
    }

    /// <summary>Сохраняет изменяемые поля заказа: статус, дату изменения статуса, мастера и диагноз.</summary>
    public void UpdateOrderState(Order order)
    {
        var entry = Context.Entry(order);
        if (entry.State == EntityState.Detached)
            Context.Orders.Attach(order);
        entry = Context.Entry(order);
        entry.Property(o => o.Status).IsModified = true;
        entry.Property(o => o.StatusChangedDate).IsModified = true;
        entry.Property(o => o.MasterId).IsModified = true;
        entry.Property(o => o.Diagnosis).IsModified = true;
        Context.SaveChanges();
    }

    public Order? GetOrder(int id) => OrdersWithDetails().FirstOrDefault(o => o.Id == id);

    public List<Order> GetOrders() => OrdersWithDetails().OrderByDescending(o => o.Id).ToList();

    public List<Order> GetOrdersByClient(int clientId) =>
        OrdersWithDetails().Where(o => o.ClientId == clientId).OrderByDescending(o => o.Id).ToList();
}

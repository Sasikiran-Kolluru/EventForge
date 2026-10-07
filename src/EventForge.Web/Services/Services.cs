using EventForge.Web.Data;
using EventForge.Web.Models;
using EventForge.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Web.Services;

public interface IAuditService
{
    Task LogAsync(string userId, string action, string entityName, int recordId, string? oldValue = null, string? newValue = null, string ipAddress = "127.0.0.1");
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    public AuditService(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(string userId, string action, string entityName, int recordId, string? oldValue = null, string? newValue = null, string ipAddress = "127.0.0.1")
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedDate = DateTime.UtcNow,
            IpAddress = ipAddress
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}

public interface ICustomerService
{
    Task<(bool Success, string Message, Customer? Data)> CreateCustomerAsync(Customer customer, string currentUserId, string userRole);
    Task<(bool Success, string Message)> UpdateCustomerAsync(Customer customer, string currentUserId, string userRole);
    Task<IEnumerable<Customer>> GetCustomersAsync(string currentUserId, string userRole, string? search);
    Task<Customer?> GetByIdAsync(int id, string currentUserId, string userRole);
}

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public CustomerService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<(bool Success, string Message, Customer? Data)> CreateCustomerAsync(Customer customer, string currentUserId, string userRole)
    {
        if (await _db.Customers.AnyAsync(c => c.Email.ToLower() == customer.Email.ToLower()))
            return (false, "A customer with this email address already exists.", null);

        if (await _db.Customers.AnyAsync(c => c.Phone == customer.Phone))
            return (false, "A customer with this phone number already exists.", null);

        customer.CustomerCode = $"CUST-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20];
        customer.CreatedBy = currentUserId;
        customer.CreatedDate = DateTime.UtcNow;

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(currentUserId, "Create", "Customer", customer.CustomerId, null, customer.CustomerName);
        return (true, "Customer created successfully.", customer);
    }

    public async Task<(bool Success, string Message)> UpdateCustomerAsync(Customer customer, string currentUserId, string userRole)
    {
        var existing = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
        if (existing is null || (userRole == "Sales Executive" && existing.CreatedBy != currentUserId))
            return (false, "Customer not found or you do not have access.");

        if (await _db.Customers.AnyAsync(c => c.CustomerId != customer.CustomerId && c.Email.ToLower() == customer.Email.ToLower()))
            return (false, "A customer with this email address already exists.");
        if (await _db.Customers.AnyAsync(c => c.CustomerId != customer.CustomerId && c.Phone == customer.Phone))
            return (false, "A customer with this phone number already exists.");

        var oldName = existing.CustomerName;
        existing.CustomerName = customer.CustomerName;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.CompanyName = customer.CompanyName;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.State = customer.State;
        existing.Status = customer.Status;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(currentUserId, "Update", "Customer", existing.CustomerId, oldName, existing.CustomerName);
        return (true, "Customer updated successfully.");
    }

    public async Task<IEnumerable<Customer>> GetCustomersAsync(string currentUserId, string userRole, string? search)
    {
        var query = _db.Customers.AsQueryable();

        if (userRole == "Sales Executive")
        {
            query = query.Where(c => c.CreatedBy == currentUserId || c.Opportunities.Any(o => o.AssignedTo == currentUserId));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(search) ||
                                     c.Email.ToLower().Contains(search) ||
                                     c.Phone.Contains(search) ||
                                     (c.CompanyName != null && c.CompanyName.ToLower().Contains(search)));
        }

        return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id, string currentUserId, string userRole)
    {
        var customer = await _db.Customers
            .Include(c => c.Opportunities)
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null) return null;

        if (userRole == "Sales Executive" && customer.CreatedBy != currentUserId && !customer.Opportunities.Any(o => o.AssignedTo == currentUserId))
        {
            return null;
        }

        return customer;
    }
}

public interface IOpportunityService
{
    Task<(bool Success, string Message)> CreateOpportunityAsync(Opportunity opp, string currentUserId);
}

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public OpportunityService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<(bool Success, string Message)> CreateOpportunityAsync(Opportunity opp, string currentUserId)
    {
        if (opp.Amount <= 0)
            return (false, "Opportunity Amount must be greater than zero.");

        if (opp.Probability < 0 || opp.Probability > 100)
            return (false, "Probability must be between 0 and 100.");

        if (opp.Status == "Open" && opp.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            return (false, "Expected close date cannot be in the past for active opportunities.");

        _db.Opportunities.Add(opp);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(currentUserId, "Create", "Opportunity", opp.OpportunityId, null, opp.OpportunityName);
        return (true, "Opportunity created successfully.");
    }
}

public interface IFollowUpService
{
    Task<(bool Success, string Message)> ScheduleFollowUpAsync(FollowUp followUp, string currentUserId);
}

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _db;

    public FollowUpService(ApplicationDbContext db) => _db = db;

    public async Task<(bool Success, string Message)> ScheduleFollowUpAsync(FollowUp followUp, string currentUserId)
    {
        if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "Follow-up date cannot be set in the past for new/planned follow-ups.");
        }

        _db.FollowUps.Add(followUp);
        await _db.SaveChangesAsync();
        return (true, "Follow-up scheduled successfully.");
    }
}

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardMetricsAsync(string userId, string role, string dateFilter = "This Month");
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db) => _db = db;

    public async Task<DashboardViewModel> GetDashboardMetricsAsync(string userId, string role, string dateFilter = "This Month")
    {
        var now = DateTime.UtcNow;
        var startDate = dateFilter switch
        {
            "This Month" => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            "This Year" => new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => DateTime.MinValue
        };

        var oppQuery = _db.Opportunities.AsQueryable();
        var leadQuery = _db.Leads.AsQueryable();
        var eventQuery = _db.Events.AsQueryable();
        var customerQuery = _db.Customers.AsQueryable();
        var followUpQuery = _db.FollowUps.AsQueryable();
        if (startDate > DateTime.MinValue)
        {
            oppQuery = oppQuery.Where(o => o.CreatedDate >= startDate);
            leadQuery = leadQuery.Where(l => l.CreatedDate >= startDate);
            eventQuery = eventQuery.Where(e => e.CreatedDate >= startDate);
            customerQuery = customerQuery.Where(c => c.CreatedDate >= startDate);
            followUpQuery = followUpQuery.Where(f => f.FollowUpDate >= startDate);
        }

        if (role == "Sales Executive")
        {
            oppQuery = oppQuery.Where(o => o.AssignedTo == userId);
            leadQuery = leadQuery.Where(l => l.AssignedTo == userId);
            eventQuery = eventQuery.Where(e => e.CreatedBy == userId);
            customerQuery = customerQuery.Where(c => c.CreatedBy == userId || c.Opportunities.Any(o => o.AssignedTo == userId));
            followUpQuery = followUpQuery.Where(f => f.AssignedTo == userId);
        }

        var vm = new DashboardViewModel
        {
            TotalCustomers = await customerQuery.CountAsync(),
            TotalLeads = await leadQuery.CountAsync(),
            OpenLeads = await leadQuery.CountAsync(l => l.Status == "New" || l.Status == "Contacted"),
            TotalOpportunities = await oppQuery.CountAsync(),
            OpenOpportunities = await oppQuery.CountAsync(o => o.Status == "Open"),
            WonOpportunities = await oppQuery.CountAsync(o => o.Stage == "Won"),
            LostOpportunities = await oppQuery.CountAsync(o => o.Stage == "Lost"),
            TotalPipelineValue = 0,
            WeightedPipelineValue = 0,

            UpcomingEventsCount = await eventQuery.CountAsync(e => e.EventDate >= now.Date && e.Status != "Cancelled" && e.Status != "Completed"),
            ConfirmedEventsCount = await eventQuery.CountAsync(e => e.Status == "Confirmed"),
            PendingFollowUpsCount = await followUpQuery.CountAsync(f => f.Status == "Planned" && f.FollowUpDate >= now.Date),

            LeadStatusBreakdown = await leadQuery.GroupBy(l => l.Status).ToDictionaryAsync(g => g.Key, g => g.Count()),
            EventStatusBreakdown = await eventQuery.GroupBy(e => e.Status).ToDictionaryAsync(g => g.Key, g => g.Count()),
            EventTypeBreakdown = await eventQuery.GroupBy(e => e.EventType).ToDictionaryAsync(g => g.Key, g => g.Count())
        };

        var opportunities = await oppQuery.AsNoTracking().Select(o => new { o.Stage, o.Status, o.Amount, o.Probability }).ToListAsync();
        var openOpportunities = opportunities.Where(o => o.Status == "Open").ToList();
        vm.TotalPipelineValue = openOpportunities.Sum(o => o.Amount);
        vm.WeightedPipelineValue = openOpportunities.Sum(o => o.Amount * o.Probability / 100m);
        vm.PipelineStageBreakdown = openOpportunities
            .GroupBy(o => o.Stage)
            .ToDictionary(group => group.Key, group => group.Sum(o => o.Amount));

        return vm;
    }
}
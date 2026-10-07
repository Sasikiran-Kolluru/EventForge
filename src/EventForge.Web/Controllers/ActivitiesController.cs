using EventForge.Web.Data;
using EventForge.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Web.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _db;
    public ActivitiesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        IQueryable<Activity> query = _db.Activities.AsNoTracking().Include(item => item.Customer).Include(item => item.Lead).Include(item => item.Event);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(item => item.AssignedTo == userId);
        }
        return View(await query.OrderByDescending(item => item.ActivityDate).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadRelatedRecords();
        var now = DateTime.Now;
        return View(new Activity { ActivityDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Activity activity)
    {
        ModelState.Remove(nameof(Activity.Customer));
        ModelState.Remove(nameof(Activity.Lead));
        ModelState.Remove(nameof(Activity.Event));
        var relatedCount = (activity.CustomerId.HasValue ? 1 : 0) + (activity.LeadId.HasValue ? 1 : 0) + (activity.EventId.HasValue ? 1 : 0);
        if (relatedCount != 1)
            ModelState.AddModelError(string.Empty, "Choose exactly one customer, lead, or event.");
        if (activity.CustomerId.HasValue && !await CanAccessCustomer(activity.CustomerId.Value))
            ModelState.AddModelError(nameof(Activity.CustomerId), "Choose a customer you can access.");
        if (activity.LeadId.HasValue && !await CanAccessLead(activity.LeadId.Value))
            ModelState.AddModelError(nameof(Activity.LeadId), "Choose a lead you can access.");
        if (activity.EventId.HasValue && !await CanAccessEvent(activity.EventId.Value))
            ModelState.AddModelError(nameof(Activity.EventId), "Choose an event you can access.");
        if (!ModelState.IsValid)
        {
            await LoadRelatedRecords(activity.CustomerId, activity.LeadId, activity.EventId);
            return View(activity);
        }
        activity.AssignedTo = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        activity.Status = "Completed";
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Activity logged successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadRelatedRecords(int? customerId = null, int? leadId = null, int? eventId = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        IQueryable<Customer> customers = _db.Customers.AsNoTracking();
        IQueryable<Lead> leads = _db.Leads.AsNoTracking();
        IQueryable<Event> events = _db.Events.AsNoTracking();
        if (User.IsInRole("Sales Executive"))
        {
            customers = customers.Where(item => item.CreatedBy == userId || item.Opportunities.Any(opportunity => opportunity.AssignedTo == userId));
            leads = leads.Where(item => item.AssignedTo == userId);
            events = events.Where(item => item.CreatedBy == userId);
        }
        ViewBag.Customers = new SelectList(await customers.OrderBy(item => item.CustomerName).ToListAsync(), nameof(Customer.CustomerId), nameof(Customer.CustomerName), customerId);
        ViewBag.Leads = new SelectList(await leads.OrderBy(item => item.LeadName).ToListAsync(), nameof(Lead.LeadId), nameof(Lead.LeadName), leadId);
        ViewBag.Events = new SelectList(await events.OrderBy(item => item.EventName).ToListAsync(), nameof(Event.EventId), nameof(Event.EventName), eventId);
    }

    private async Task<bool> CanAccessCustomer(int id)
    {
        IQueryable<Customer> query = _db.Customers.Where(item => item.CustomerId == id);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(item => item.CreatedBy == userId || item.Opportunities.Any(opportunity => opportunity.AssignedTo == userId));
        }
        return await query.AnyAsync();
    }

    private async Task<bool> CanAccessLead(int id)
    {
        var query = _db.Leads.Where(item => item.LeadId == id);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(item => item.AssignedTo == userId);
        }
        return await query.AnyAsync();
    }

    private async Task<bool> CanAccessEvent(int id)
    {
        var query = _db.Events.Where(item => item.EventId == id);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(item => item.CreatedBy == userId);
        }
        return await query.AnyAsync();
    }
}

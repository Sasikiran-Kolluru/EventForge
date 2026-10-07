using EventForge.Web.Data;
using EventForge.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Web.Controllers;

[Authorize]
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _db;
    public FollowUpsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        IQueryable<FollowUp> query = _db.FollowUps.AsNoTracking().Include(item => item.Customer).Include(item => item.Lead).Include(item => item.Event);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(item => item.AssignedTo == userId);
        }
        return View(await query.OrderBy(item => item.FollowUpDate).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadRelatedRecords();
        return View(new FollowUp { FollowUpDate = DateTime.Today.AddDays(1) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp followUp)
    {
        ModelState.Remove(nameof(FollowUp.Customer));
        ModelState.Remove(nameof(FollowUp.Lead));
        ModelState.Remove(nameof(FollowUp.Event));
        ValidateTarget(followUp);
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(FollowUp.FollowUpDate), "A planned follow-up cannot be scheduled in the past.");
        if (!ModelState.IsValid)
        {
            await LoadRelatedRecords(followUp.CustomerId, followUp.LeadId, followUp.EventId);
            return View(followUp);
        }
        followUp.AssignedTo = userId;
        _db.FollowUps.Add(followUp);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var query = _db.FollowUps.Where(item => item.FollowUpId == id);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(item => item.AssignedTo == userId);
        }
        var followUp = await query.FirstOrDefaultAsync();
        if (followUp is null) return NotFound();
        if (followUp.Status == "Planned")
        {
            followUp.Status = "Completed";
            await _db.SaveChangesAsync();
        }
        TempData["SuccessMessage"] = "Follow-up marked complete.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateTarget(FollowUp followUp)
    {
        var count = (followUp.CustomerId.HasValue ? 1 : 0) + (followUp.LeadId.HasValue ? 1 : 0) + (followUp.EventId.HasValue ? 1 : 0);
        if (count != 1) ModelState.AddModelError(string.Empty, "Choose exactly one customer, lead, or event for this follow-up.");
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
}

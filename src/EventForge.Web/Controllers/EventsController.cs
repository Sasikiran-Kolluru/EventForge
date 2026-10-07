using EventForge.Web.Data;
using EventForge.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Web.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _db;
    public EventsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        IQueryable<Event> query = _db.Events.AsNoTracking().Include(item => item.Customer);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(item => item.CreatedBy == userId);
        }
        return View(await query.OrderBy(item => item.EventDate).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCustomers();
        return View(new Event { EventDate = DateTime.Today.AddMonths(1), ExpectedGuests = 50 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Event item)
    {
        ModelState.Remove(nameof(Event.EventCode));
        ModelState.Remove(nameof(Event.Customer));
        ModelState.Remove(nameof(Event.Opportunity));
        if (item.CustomerId <= 0 || !await CanAccessCustomer(item.CustomerId))
            ModelState.AddModelError(nameof(Event.CustomerId), "Choose a customer you can access.");
        if (item.ExpectedGuests < 1)
            ModelState.AddModelError(nameof(Event.ExpectedGuests), "Guest count must be at least one.");
        if (item.Status is "Planning" or "Confirmed" or "In Progress" && item.EventDate.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(Event.EventDate), "A planned event cannot be scheduled in the past.");
        if (!ModelState.IsValid)
        {
            await LoadCustomers(item.CustomerId);
            return View(item);
        }
        item.EventCode = $"EVT-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        item.CreatedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        item.CreatedDate = DateTime.UtcNow;
        _db.Events.Add(item);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Event created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await FindAccessibleEvent(id);
        if (item is null) return NotFound();
        await LoadCustomers(item.CustomerId);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Event form)
    {
        if (id != form.EventId) return BadRequest();
        var item = await FindAccessibleEvent(id);
        if (item is null) return NotFound();
        ModelState.Remove(nameof(Event.EventCode));
        ModelState.Remove(nameof(Event.Customer));
        ModelState.Remove(nameof(Event.Opportunity));
        if (form.CustomerId <= 0 || !await CanAccessCustomer(form.CustomerId))
            ModelState.AddModelError(nameof(Event.CustomerId), "Choose a customer you can access.");
        if (form.ExpectedGuests < 1)
            ModelState.AddModelError(nameof(Event.ExpectedGuests), "Guest count must be at least one.");
        if (form.Status is "Planning" or "Confirmed" or "In Progress" && form.EventDate.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(Event.EventDate), "A planned event cannot be scheduled in the past.");
        if (!ModelState.IsValid)
        {
            await LoadCustomers(form.CustomerId);
            return View(form);
        }
        item.EventName = form.EventName;
        item.EventType = form.EventType;
        item.CustomerId = form.CustomerId;
        item.EventDate = form.EventDate;
        item.ExpectedGuests = form.ExpectedGuests;
        item.Venue = form.Venue;
        item.City = form.City;
        item.Budget = form.Budget;
        item.Status = form.Status;
        item.Notes = form.Notes;
        item.ModifiedDate = DateTime.UtcNow;
        item.ModifiedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Event updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCustomers(int? selectedId = null)
    {
        IQueryable<Customer> customers = _db.Customers.AsNoTracking();
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            customers = customers.Where(item => item.CreatedBy == userId || item.Opportunities.Any(opportunity => opportunity.AssignedTo == userId));
        }
        ViewBag.Customers = new SelectList(await customers.OrderBy(item => item.CustomerName).ToListAsync(), nameof(Customer.CustomerId), nameof(Customer.CustomerName), selectedId);
    }

    private async Task<bool> CanAccessCustomer(int id)
    {
        IQueryable<Customer> customers = _db.Customers.Where(item => item.CustomerId == id);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            customers = customers.Where(item => item.CreatedBy == userId || item.Opportunities.Any(opportunity => opportunity.AssignedTo == userId));
        }
        return await customers.AnyAsync();
    }

    private Task<Event?> FindAccessibleEvent(int id)
    {
        var query = _db.Events.Where(item => item.EventId == id);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(item => item.CreatedBy == userId);
        }
        return query.FirstOrDefaultAsync();
    }
}

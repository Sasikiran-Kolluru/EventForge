using EventForge.Web.Data;
using EventForge.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Web.Controllers;

[Authorize]
public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _db;
    public OpportunitiesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        IQueryable<Opportunity> query = _db.Opportunities.AsNoTracking().Include(item => item.Customer);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(item => item.AssignedTo == userId);
        }
        return View(await query.OrderByDescending(item => item.CreatedDate).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCustomers();
        return View(new Opportunity { ExpectedCloseDate = DateTime.Today.AddMonths(1) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Opportunity opportunity)
    {
        ModelState.Remove(nameof(Opportunity.Customer));
        ModelState.Remove(nameof(Opportunity.WeightedValue));
        if (opportunity.CustomerId <= 0 || !await CanAccessCustomer(opportunity.CustomerId))
            ModelState.AddModelError(nameof(Opportunity.CustomerId), "Choose a customer you can access.");
        if (opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date && opportunity.Stage is not "Won" and not "Lost")
            ModelState.AddModelError(nameof(Opportunity.ExpectedCloseDate), "Expected close date must not be in the past for an open opportunity.");
        if (!ModelState.IsValid)
        {
            await LoadCustomers(opportunity.CustomerId);
            return View(opportunity);
        }
        opportunity.AssignedTo = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        opportunity.CreatedDate = DateTime.UtcNow;
        SetStatus(opportunity);
        _db.Opportunities.Add(opportunity);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Opportunity created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var opportunity = await FindAccessibleOpportunity(id);
        if (opportunity is null) return NotFound();
        await LoadCustomers(opportunity.CustomerId);
        return View(opportunity);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Opportunity form)
    {
        if (id != form.OpportunityId) return BadRequest();
        var opportunity = await FindAccessibleOpportunity(id);
        if (opportunity is null) return NotFound();
        ModelState.Remove(nameof(Opportunity.Customer));
        ModelState.Remove(nameof(Opportunity.WeightedValue));
        if (form.CustomerId <= 0 || !await CanAccessCustomer(form.CustomerId))
            ModelState.AddModelError(nameof(Opportunity.CustomerId), "Choose a customer you can access.");
        if (form.ExpectedCloseDate.Date < DateTime.UtcNow.Date && form.Stage is not "Won" and not "Lost")
            ModelState.AddModelError(nameof(Opportunity.ExpectedCloseDate), "Expected close date must not be in the past for an open opportunity.");
        if (!ModelState.IsValid)
        {
            await LoadCustomers(form.CustomerId);
            return View(form);
        }
        opportunity.OpportunityName = form.OpportunityName;
        opportunity.CustomerId = form.CustomerId;
        opportunity.Amount = form.Amount;
        opportunity.Stage = form.Stage;
        opportunity.Probability = form.Probability;
        opportunity.ExpectedCloseDate = form.ExpectedCloseDate;
        opportunity.Notes = form.Notes;
        SetStatus(opportunity);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Opportunity updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCustomers(int? selectedId = null)
    {
        var customers = _db.Customers.AsNoTracking();
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            customers = customers.Where(item => item.CreatedBy == userId || item.Opportunities.Any(opportunity => opportunity.AssignedTo == userId));
        }
        ViewBag.Customers = new SelectList(await customers.OrderBy(item => item.CustomerName).ToListAsync(), nameof(Customer.CustomerId), nameof(Customer.CustomerName), selectedId);
    }

    private async Task<bool> CanAccessCustomer(int id)
    {
        var query = _db.Customers.Where(item => item.CustomerId == id);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(item => item.CreatedBy == userId || item.Opportunities.Any(opportunity => opportunity.AssignedTo == userId));
        }
        return await query.AnyAsync();
    }

    private Task<Opportunity?> FindAccessibleOpportunity(int id)
    {
        var query = _db.Opportunities.Where(item => item.OpportunityId == id);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(item => item.AssignedTo == userId);
        }
        return query.FirstOrDefaultAsync();
    }

    private static void SetStatus(Opportunity opportunity)
    {
        opportunity.Status = opportunity.Stage switch
        {
            "Won" => "Won",
            "Lost" => "Lost",
            _ => "Open"
        };
        if (opportunity.Stage == "Won") opportunity.Probability = 100;
        if (opportunity.Stage == "Lost") opportunity.Probability = 0;
    }
}

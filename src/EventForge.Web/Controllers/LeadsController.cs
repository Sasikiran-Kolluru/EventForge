using EventForge.Web.Data;
using EventForge.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Web.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly ApplicationDbContext _db;
    public LeadsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Leads.AsNoTracking();
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(lead => lead.AssignedTo == userId);
        }
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(lead => lead.LeadName.Contains(search) || lead.Email.Contains(search) || (lead.CompanyName != null && lead.CompanyName.Contains(search)));
        ViewData["Search"] = search;
        return View(await query.OrderByDescending(lead => lead.CreatedDate).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new Lead { PreferredDate = DateTime.Today.AddMonths(1) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead lead)
    {
        ModelState.Remove(nameof(Lead.LeadCode));
        if (!ModelState.IsValid) return View(lead);
        lead.LeadCode = $"LEAD-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        lead.AssignedTo = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        lead.CreatedDate = DateTime.UtcNow;
        lead.Status = "New";
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Lead created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await FindAccessibleLead(id);
        return lead is null ? NotFound() : View(lead);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead form)
    {
        if (id != form.LeadId) return BadRequest();
        var lead = await FindAccessibleLead(id);
        if (lead is null) return NotFound();
        ModelState.Remove(nameof(Lead.LeadCode));
        if (!ModelState.IsValid) return View(form);
        lead.LeadName = form.LeadName;
        lead.Email = form.Email;
        lead.Phone = form.Phone;
        lead.CompanyName = form.CompanyName;
        lead.Source = form.Source;
        lead.Status = form.Status;
        lead.EventType = form.EventType;
        lead.ExpectedGuests = form.ExpectedGuests;
        lead.PreferredDate = form.PreferredDate;
        lead.Budget = form.Budget;
        lead.Requirements = form.Requirements;
        lead.Priority = form.Priority;
        lead.Notes = form.Notes;
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Lead updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await FindAccessibleLead(id);
        if (lead is null) return NotFound();
        if (lead.Status == "Converted")
        {
            TempData["ErrorMessage"] = "This lead has already been converted.";
            return RedirectToAction(nameof(Index));
        }
        var amount = lead.Budget > 0 ? lead.Budget : lead.ExpectedValue;
        if (amount <= 0)
        {
            TempData["ErrorMessage"] = "Add a positive budget or expected value before converting this lead.";
            return RedirectToAction(nameof(Index));
        }
        var email = lead.Email.Trim();
        var phone = lead.Phone.Trim();
        if (await _db.Customers.AnyAsync(customer => customer.Email.ToLower() == email.ToLower() || customer.Phone == phone))
        {
            TempData["ErrorMessage"] = "A customer with this email or phone already exists. Review the customer record before converting this lead.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var customer = new Customer
        {
            CustomerCode = $"CUST-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            CustomerName = lead.LeadName,
            Email = email,
            Phone = phone,
            CompanyName = lead.CompanyName,
            Status = "Active",
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        var closeDate = lead.PreferredDate?.Date >= DateTime.UtcNow.Date
            ? lead.PreferredDate.Value.Date
            : DateTime.UtcNow.Date.AddDays(30);
        _db.Opportunities.Add(new Opportunity
        {
            OpportunityName = $"{lead.EventType} - {lead.CompanyName ?? lead.LeadName}",
            CustomerId = customer.CustomerId,
            LeadId = lead.LeadId,
            Amount = amount,
            Stage = "Qualification",
            Probability = 20,
            ExpectedCloseDate = closeDate,
            Status = "Open",
            AssignedTo = userId,
            CreatedDate = DateTime.UtcNow,
            Notes = lead.Requirements
        });
        lead.Status = "Converted";
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["SuccessMessage"] = "Lead converted to a customer and opportunity.";
        return RedirectToAction(nameof(Index));
    }

    private Task<Lead?> FindAccessibleLead(int id)
    {
        var query = _db.Leads.Where(lead => lead.LeadId == id);
        if (User.IsInRole("Sales Executive"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
            query = query.Where(lead => lead.AssignedTo == userId);
        }
        return query.FirstOrDefaultAsync();
    }
}

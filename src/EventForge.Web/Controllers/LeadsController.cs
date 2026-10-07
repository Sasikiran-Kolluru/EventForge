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

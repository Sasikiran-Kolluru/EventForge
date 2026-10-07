using EventForge.Web.Data;
using EventForge.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Web.Controllers;

[Authorize]
public class VendorsController : Controller
{
    private readonly ApplicationDbContext _db;
    public VendorsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Vendors.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(item => item.VendorName.Contains(search) || item.Category.Contains(search) || item.City.Contains(search));
        ViewData["Search"] = search;
        return View(await query.OrderBy(item => item.VendorName).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new Vendor());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Vendor vendor)
    {
        ModelState.Remove(nameof(Vendor.VendorCode));
        if (!ModelState.IsValid) return View(vendor);
        vendor.VendorCode = $"VND-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Vendor created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vendor = await _db.Vendors.FindAsync(id);
        return vendor is null ? NotFound() : View(vendor);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Vendor form)
    {
        if (id != form.VendorId) return BadRequest();
        var vendor = await _db.Vendors.FindAsync(id);
        if (vendor is null) return NotFound();
        ModelState.Remove(nameof(Vendor.VendorCode));
        if (!ModelState.IsValid) return View(form);
        vendor.VendorName = form.VendorName;
        vendor.Category = form.Category;
        vendor.ContactPerson = form.ContactPerson;
        vendor.Email = form.Email;
        vendor.Phone = form.Phone;
        vendor.City = form.City;
        vendor.Rating = form.Rating;
        vendor.Status = form.Status;
        vendor.Notes = form.Notes;
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Vendor updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}

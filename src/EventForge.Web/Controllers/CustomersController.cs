using EventForge.Web.Models;
using EventForge.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Web.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : User.IsInRole("Sales Executive") ? "Sales Executive" : "None";

        var list = await _customerService.GetCustomersAsync(userId, role, search);
        ViewData["Search"] = search;
        return View(list);
    }

    public IActionResult Create() => View(new Customer());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        ModelState.Remove(nameof(Customer.CustomerCode));
        if (!ModelState.IsValid) return View(customer);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : User.IsInRole("Sales Executive") ? "Sales Executive" : "None";

        var (success, message, data) = await _customerService.CreateCustomerAsync(customer, userId, role);
        if (!success)
        {
            ModelState.AddModelError("", message);
            return View(customer);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : User.IsInRole("Sales Executive") ? "Sales Executive" : "None";

        var customer = await _customerService.GetByIdAsync(id, userId, role);
        if (customer == null) return NotFound();

        return View(customer);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : User.IsInRole("Sales Executive") ? "Sales Executive" : "None";
        var customer = await _customerService.GetByIdAsync(id, userId, role);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.CustomerId) return BadRequest();
        ModelState.Remove(nameof(Customer.CustomerCode));
        if (!ModelState.IsValid) return View(customer);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : "Sales Executive";
        var (success, message) = await _customerService.UpdateCustomerAsync(customer, userId, role);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(customer);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id });
    }
}
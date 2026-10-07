using EventForge.Web.DTOs;
using EventForge.Web.Models;
using EventForge.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Web.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersApiController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : "Sales Executive";

        var list = await _customerService.GetCustomersAsync(userId, role, search);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : "Sales Executive";

        var customer = await _customerService.GetByIdAsync(id, userId, role);
        if (customer == null) return NotFound(new { message = "Customer not found or unauthorized access." });

        return Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : "Sales Executive";
        var customer = new Customer
        {
            CustomerName = request.CustomerName,
            Email = request.Email,
            Phone = request.Phone,
            CompanyName = request.CompanyName,
            Address = request.Address,
            City = request.City,
            State = request.State
        };

        var (success, message, data) = await _customerService.CreateCustomerAsync(customer, userId, role);
        if (!success) return BadRequest(new { message });

        return CreatedAtAction(nameof(GetById), new { id = data!.CustomerId }, data);
    }
}
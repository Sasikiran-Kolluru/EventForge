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
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : User.IsInRole("Sales Executive") ? "Sales Executive" : "None";

        var list = await _customerService.GetCustomersAsync(userId, role, search);
        return Ok(list.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : User.IsInRole("Sales Executive") ? "Sales Executive" : "None";

        var customer = await _customerService.GetByIdAsync(id, userId, role);
        if (customer == null) return NotFound(new { message = "Customer not found or unauthorized access." });

        return Ok(ToDto(customer));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : User.IsInRole("Sales Executive") ? "Sales Executive" : "None";
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

        return CreatedAtAction(nameof(GetById), new { id = data!.CustomerId }, ToDto(data));
    }

    private static EventForge.Web.DTOs.CustomerDto ToDto(Customer customer) => new()
    {
        CustomerId = customer.CustomerId,
        CustomerCode = customer.CustomerCode,
        CustomerName = customer.CustomerName,
        Email = customer.Email,
        Phone = customer.Phone,
        CompanyName = customer.CompanyName,
        City = customer.City,
        Status = customer.Status
    };
}
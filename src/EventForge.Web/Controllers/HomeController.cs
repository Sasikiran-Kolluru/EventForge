using EventForge.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;

    public HomeController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(string dateFilter = "All Time")
    {
        var validFilters = new[] { "This Month", "This Year", "All Time" };
        if (!validFilters.Contains(dateFilter, StringComparer.Ordinal))
        {
            dateFilter = "All Time";
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var role = User.IsInRole("Admin") ? "Admin" : User.IsInRole("Manager") ? "Manager" : User.IsInRole("Sales Executive") ? "Sales Executive" : "None";

        var metrics = await _dashboardService.GetDashboardMetricsAsync(userId, role, dateFilter);
        ViewData["DateFilter"] = dateFilter;
        return View(metrics);
    }
}
using System.ComponentModel.DataAnnotations;

namespace EventForge.Web.ViewModels;

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }

    public int UpcomingEventsCount { get; set; }
    public int ConfirmedEventsCount { get; set; }
    public int PendingFollowUpsCount { get; set; }

    public Dictionary<string, int> LeadStatusBreakdown { get; set; } = new();
    public Dictionary<string, decimal> PipelineStageBreakdown { get; set; } = new();
    public Dictionary<string, int> EventStatusBreakdown { get; set; } = new();
    public Dictionary<string, int> EventTypeBreakdown { get; set; } = new();
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
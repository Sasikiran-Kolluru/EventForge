using System.ComponentModel.DataAnnotations;

namespace EventForge.Web.Models;

public class Opportunity
{
    public int OpportunityId { get; set; }

    [Required, StringLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 20;

    [Required]
    public DateTime ExpectedCloseDate { get; set; }

    public string Status { get; set; } = "Open"; // Open, Won, Lost
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string AssignedTo { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public decimal WeightedValue => Amount * Probability / 100m;
}
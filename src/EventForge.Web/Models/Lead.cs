using System.ComponentModel.DataAnnotations;

namespace EventForge.Web.Models;

public class Lead
{
    public int LeadId { get; set; }

    [Required, StringLength(20)]
    public string LeadCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LeadName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CompanyName { get; set; }

    public string Source { get; set; } = "Web Enquiry";
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [Range(0, 10000000)]
    public decimal ExpectedValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string AssignedTo { get; set; } = string.Empty;

    // EventForge Specific Domain Attributes
    public string EventType { get; set; } = "Corporate Conference";
    public string? Requirements { get; set; }
    public int ExpectedGuests { get; set; } = 50;
    public DateTime? PreferredDate { get; set; }
    public decimal Budget { get; set; }
    public string Priority { get; set; } = "Medium";
    public string? Notes { get; set; }
}
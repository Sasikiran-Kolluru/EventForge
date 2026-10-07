using System.ComponentModel.DataAnnotations;

namespace EventForge.Web.Models;

public class FollowUp
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public int? EventId { get; set; }
    public Event? Event { get; set; }

    [Required]
    public DateTime FollowUpDate { get; set; }

    public string FollowUpType { get; set; } = "Call";
    public string Remarks { get; set; } = string.Empty;
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled
    public string AssignedTo { get; set; } = string.Empty;
}
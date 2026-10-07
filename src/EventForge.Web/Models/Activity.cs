using System.ComponentModel.DataAnnotations;

namespace EventForge.Web.Models;

public class Activity
{
    public int ActivityId { get; set; }
    [Required, StringLength(30)]
    public string ActivityType { get; set; } = "Call";
    [Required, StringLength(150)]
    public string Subject { get; set; } = string.Empty;
    [StringLength(2000)]
    public string? Description { get; set; }
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public int? EventId { get; set; }
    public Event? Event { get; set; }

    public string AssignedTo { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
}
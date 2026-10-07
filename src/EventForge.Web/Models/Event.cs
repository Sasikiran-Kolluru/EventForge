using System.ComponentModel.DataAnnotations;

namespace EventForge.Web.Models;

public class Event
{
    public int EventId { get; set; }

    [Required, StringLength(20)]
    public string EventCode { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    public string EventType { get; set; } = "Corporate Conference";

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    public int ExpectedGuests { get; set; }

    [Required, StringLength(200)]
    public string Venue { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string City { get; set; } = string.Empty;

    [Range(0, 10000000)]
    public decimal Budget { get; set; }

    public string Status { get; set; } = "Planning"; // Planning, Confirmed, In Progress, Completed, Cancelled
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }

    public ICollection<EventVendor> EventVendors { get; set; } = new List<EventVendor>();
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
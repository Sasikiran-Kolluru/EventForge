using System.ComponentModel.DataAnnotations;

namespace EventForge.Web.Models;

public class Vendor
{
    public int VendorId { get; set; }

    [Required, StringLength(20)]
    public string VendorCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string VendorName { get; set; } = string.Empty;

    public string Category { get; set; } = "Catering";
    public string ContactPerson { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string Phone { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }

    public ICollection<EventVendor> EventVendors { get; set; } = new List<EventVendor>();
}
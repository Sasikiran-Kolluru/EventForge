namespace EventForge.Web.Models;

public class EventVendor
{
    public int EventId { get; set; }
    public Event? Event { get; set; }

    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public decimal AgreedCost { get; set; }
    public string ServiceDetails { get; set; } = string.Empty;
    public string Status { get; set; } = "Confirmed";
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public enum BookingStatus { Pending, Confirmed, Cancelled, Completed }

public class Booking
{
    [Key]
    public int BookingId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty; 

    [Required]
    public int SlotId { get; set; }

    [ForeignKey("SlotId")]
    public ChargerSlot? ChargerSlot { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    [Required]
    public double BatteryCapacityKwh { get; set; }

    [Required]
    [Range(0, 100)]
    public double StartingPercentage { get; set; }

    [Required]
    [Range(0, 100)]
    public double TargetPercentage { get; set; }

    [Required]
    public double EnergyRequestedKwh { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal RatePricePerKwh { get; set; }

    public Payment? Payment { get; set; }
}
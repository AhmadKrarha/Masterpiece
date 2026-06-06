using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Materpiece.Models;

public enum ChargerType { FastDC, NormalAC }
public enum SlotStatus { Available, Occupied, Maintenance }

public class ChargerSlot
{
    [Key]
    public int SlotId { get; set; }

    [Required]
    public int StationId { get; set; }

    [ForeignKey("StationId")]
    public Station? Station { get; set; }

    [Required]
    public string SlotNumber { get; set; } = string.Empty; // e.g., "A-1", "A-2"

    [Required]
    public ChargerType Type { get; set; }

    [Required]
    public SlotStatus Status { get; set; } = SlotStatus.Available;

    [Required]
    [Range(1.0, 350.0)]
    public double PowerOutputKw { get; set; } = 7.4; // Default AC charging speed in kW

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    [Range(0.01, 10.00)]
    public decimal PricePerKwh { get; set; } = 0.12m; // Default AC price per kWh in JOD

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
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

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
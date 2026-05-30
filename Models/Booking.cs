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

    public Payment? Payment { get; set; }
}
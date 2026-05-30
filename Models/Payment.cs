using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [Required]
    public int BookingId { get; set; }

    [ForeignKey("BookingId")]
    public Booking? Booking { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(100)]
    public string StripePaymentIntentId { get; set; } = string.Empty; 

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; 

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
}
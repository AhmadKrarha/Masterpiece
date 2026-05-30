using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Materpiece.Models;

public class Review
{
    [Key]
    public int ReviewId { get; set; }

    [Required]
    public int StationId { get; set; }

    [ForeignKey("StationId")]
    public Station? Station { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty; 

    [Required]
    public int BookingId { get; set; } 

    [ForeignKey("BookingId")]
    public Booking? Booking { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
    public int Rating { get; set; }

    [Required]
    [StringLength(1000)]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
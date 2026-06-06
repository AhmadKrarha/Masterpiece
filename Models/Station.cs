using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Materpiece.Models
{
    public class Station
    {
        [Key]
        public int StationId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(9, 6)")]
        public decimal Latitude { get; set; }

        [Required]
        [Column(TypeName = "decimal(9, 6)")]
        public decimal Longitude { get; set; }

        public int TotalSlots { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;  

        public ICollection<ChargerSlot> ChargerSlots { get; set; } = new List<ChargerSlot>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
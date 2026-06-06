using System;
using System.ComponentModel.DataAnnotations;

namespace Materpiece.Models.ViewModels
{
    public class BookingReservationDto
    {
        [Required]
        public int ChargerSlotId { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        public string StartTime { get; set; } = string.Empty;

        public string? EndTime { get; set; }

        [Required]
        [Range(1.0, 150.0)]
        public double BatteryCapacityKwh { get; set; }

        [Required]
        [Range(0, 100)]
        public double CurrentChargePct { get; set; }

        [Required]
        [Range(0, 100)]
        public double TargetChargePct { get; set; }
    }
}

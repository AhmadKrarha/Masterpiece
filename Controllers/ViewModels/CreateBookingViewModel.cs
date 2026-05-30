using System;
using System.ComponentModel.DataAnnotations;

namespace Materpiece.ViewModels
{
    public class CreateBookingViewModel
    {
        [Required]
        public int SlotId { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }

        // Hidden field to redirect back to the map/station smoothly
        public int StationId { get; set; }
    }
}
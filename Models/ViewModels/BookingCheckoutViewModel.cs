using System;
using System.ComponentModel.DataAnnotations;

namespace Materpiece.Models.ViewModels
{
    public class BookingCheckoutViewModel
    {

        public int ChargerSlotId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public string LocationDescription { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public string ConnectorType { get; set; } = string.Empty;
        public double PowerOutputKw { get; set; }
        public decimal PricePerKwh { get; set; }

     
        [Required(ErrorMessage = "Please select a reservation date.")]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please select a start time.")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Please select an end time.")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }
    }
}
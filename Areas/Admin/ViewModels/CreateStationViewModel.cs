using System.ComponentModel.DataAnnotations;

namespace Materpiece.Areas.Admin.ViewModels
{
    public class CreateStationViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        [Range(-90, 90)]
        public decimal Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public decimal Longitude { get; set; }

        public string Description { get; set; }

        [Required]
        [Display(Name = "Standard AC Slots")]
        [Range(0, 50)]
        public int NormalAcSlotsCount { get; set; }

        [Required]
        [Display(Name = "Fast DC Slots")]
        [Range(0, 50)]
        public int FastDcSlotsCount { get; set; }
    }
}
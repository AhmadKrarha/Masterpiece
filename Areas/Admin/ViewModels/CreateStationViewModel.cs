using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Materpiece.Areas.Admin.ViewModels
{
    public class CreateStationViewModel
    {
        [Required]
        [Display(Name = "Station Owner")]
        public string SelectedOwnerId { get; set; }

        public SelectList? UsersList { get; set; }

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
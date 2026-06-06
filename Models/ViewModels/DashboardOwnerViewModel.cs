using Materpiece.Models;

namespace Materpiece.Models.ViewModels
{
    public class DashboardOwnerViewModel
    {
        public int TotalStations { get; set; }
        public int TotalActiveSlots { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Station> Stations { get; set; } = new List<Station>();
    }
}

using Materpiece.Models;
using Microsoft.AspNetCore.Identity;

namespace Materpiece.Models.ViewModels
{
    public class DashboardAdminViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalStations { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        
        public List<Station> RecentStations { get; set; } = new List<Station>();
    }
}

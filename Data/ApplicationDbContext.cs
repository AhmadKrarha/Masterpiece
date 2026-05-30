using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Materpiece.Models;

namespace Materpiece.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Station> Stations { get; set; }
        public DbSet<ChargerSlot> ChargerSlots { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Queue> Queues { get; set; }
        public DbSet<Review> Reviews { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); 

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Station)
                .WithMany(s => s.Reviews) 
                .HasForeignKey(r => r.StationId)
                .OnDelete(DeleteBehavior.NoAction); // <-- This breaks the cycle

           
        }
    }
   
}


using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ===== DB SETS =====
        public DbSet<Category> Categories { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingDetail> BookingDetails { get; set; }
        public DbSet<Speaker> Speakers { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Sponsor> Sponsors { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Booking 1 - N BookingDetails
            builder.Entity<BookingDetail>()
                .HasOne(bd => bd.Booking)
                .WithMany(b => b.BookingDetails)
                .HasForeignKey(bd => bd.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Booking - User
            builder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // TicketType 1 - N BookingDetail
            builder.Entity<TicketType>()
                .HasMany(t => t.BookingDetails)
                .WithOne(bd => bd.TicketType)
                .HasForeignKey(bd => bd.TicketTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Event - Category
            builder.Entity<Event>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Event 1 - N TicketType
            builder.Entity<TicketType>()
                .HasOne(t => t.Event)
                .WithMany(e => e.TicketTypes)
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Event 1 - N Speaker
            builder.Entity<Speaker>()
                .HasOne(s => s.Event)
                .WithMany(e => e.Speakers)
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Event 1 - N Schedule
            builder.Entity<Event>()
                .HasMany(e => e.Schedules)
                .WithOne(s => s.Event)
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Event 1 - N Sponsor
            builder.Entity<Sponsor>()
                .HasOne(s => s.Event)
                .WithMany(e => e.Sponsors)
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

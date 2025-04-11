using Microsoft.EntityFrameworkCore;

namespace Container_Testing.Models.Context
{
    public class ETicketingContext : DbContext
    {
        public ETicketingContext(DbContextOptions<ETicketingContext> options) : base(options) { }
        public DbSet<UserModels> CatalogUser { get; set; }
        public DbSet<AirlineModels> CatalogAirline{ get; set; }
        public DbSet<BookingModels> CatalogBooking { get; set; }
        public DbSet<FlightModels> CatalogFlight{ get; set; }
    }
}

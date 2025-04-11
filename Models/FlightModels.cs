using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Container_Testing.Models
{
    [Table("FlightsTable")]
    public class FlightModels
    {
        [Key]
        public string? FlightID { get; set; } = Guid.NewGuid().ToString();
        public string? FlightDestination { get; set; }
        public DateTime? FlightTime { get; set; }
        public DateTime? FlightArrival { get; set; }
        public string? FlightFrom { get; set; }
        public string? AirlineID { get; set; }
        public int? FlightPrice { get; set; }
        public int? FlightSeat { get; set; }
        public int? BookedSeat { get; set; }
    }
}

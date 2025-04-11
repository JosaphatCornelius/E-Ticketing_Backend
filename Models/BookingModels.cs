using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Container_Testing.Models
{
    [Table("BookingsTable")]
    public class BookingModels
    {
        [Key]
        public string? BookingID { get; set; } = Guid.NewGuid().ToString();
        public string? UserID { get; set; }
        public string? FlightID { get; set; }
        public int? BookingPrice { get; set; }
        public int? SeatAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public string? BookingConfirmation { get; set; }
    }
}

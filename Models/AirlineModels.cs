using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Container_Testing.Models
{
    [Table("AirlinesTable")]
    public class AirlineModels
    {
        [Key]
        public string? AirlineID { get; set; } = Guid.NewGuid().ToString();
        public int? TicketSold { get; set; }
    }
}

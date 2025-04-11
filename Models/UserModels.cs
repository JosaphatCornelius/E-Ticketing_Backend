using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Container_Testing.Models
{
    [Table("UserTable")]
    public class UserModels
    {
        [Key]
        public string? UserID { get; set; } = Guid.NewGuid().ToString();
        public string? Username { get; set; }
        public string? UserPassword { get; set; }
        public string? UserRole { get; set; }
        public string? UserEmail { get; set; }
        public string? UserAddress { get; set; }
        public DateTime? Birthday { get; set; }
    }
}

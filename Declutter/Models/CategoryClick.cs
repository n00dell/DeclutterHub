using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeclutterHub.Models
{
    public class CategoryClick
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }  // Foreign key to User

        [Required]
        public int CategoryId { get; set; }  // Foreign key to Category

        [Required]
        public DateTime ClickedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        // Optional: Add IP address for analytics
        public string? IpAddress { get; set; }

        // Optional: Track session ID to group clicks
        public string? SessionId { get; set; }
    }
}
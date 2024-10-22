using System.ComponentModel.DataAnnotations;

namespace DeclutterHub.Models
{
    public class ItemViewModel
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public bool IsSold { get; set; }

        public string Location { get; set; }

        [Display(Name = "Phone Number")]
        public int PhoneNumber { get; set; }

        public bool IsNegotiable { get; set; }

        public string Condition { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int UserId { get; set; }

        // Image upload field
        [Display(Name = "Upload Images")]
        public List<IFormFile> ImageFiles { get; set; }
    }
}

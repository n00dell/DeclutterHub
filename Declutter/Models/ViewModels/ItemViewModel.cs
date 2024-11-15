using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DeclutterHub.Models.ViewModels
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

        public string CountryCode { get; set; }

        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Please enter a valid 10-digit phone number.")]
        public string PhoneNumber { get; set; }

        public List<SelectListItem> CountryCodes { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "+1", Text = "US (+1)" },
        new SelectListItem { Value = "+44", Text = "UK (+44)" },
        new SelectListItem { Value = "+254", Text = "KE (+254)" },
        new SelectListItem { Value = "+91", Text = "IN (+91)" },
        // Add more country codes as needed
    };
        public bool IsNegotiable { get; set; }

        public string Condition { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string UserId { get; set; }

        // Image upload field
        [Display(Name = "Upload Images")]
        public List<IFormFile> ImageFiles { get; set; }
    }
}

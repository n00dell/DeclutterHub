using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DeclutterHub.Models
{
    public class EditItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; }
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Please enter a valid 10-digit phone number.")]
        public string PhoneNumber { get; set; }
        public bool IsNegotiable { get; set; }
        public string Condition { get; set; }
        public bool IsVerified { get; set; }
        public int CategoryId { get; set; }
        public bool IsSold { get; set; }
        public string CountryCode { get; set; }
        public List<SelectListItem> CountryCodes { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "+1", Text = "US (+1)" },
        new SelectListItem { Value = "+44", Text = "UK (+44)" },
        new SelectListItem { Value = "+254", Text = "KE (+254)" },
        new SelectListItem { Value = "+91", Text = "IN (+91)" },
        // Add more country codes as needed
    };
        public ICollection<Image> Images { get; set; }
        public List<IFormFile>? NewImages { get; set; }  // Add this property for new image uploads
        public List<int>? ImagesToDelete { get; set; }
        public EditItemViewModel()
        {
            // Initialize collections to empty lists to avoid null reference exceptions
            Images = new List<Image>();
            NewImages = new List<IFormFile>();
            ImagesToDelete = new List<int>();
        }
    }
}

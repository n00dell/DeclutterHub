using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DeclutterHub.Models
{
    public class CategoryViewModel
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        // For uploading an image
        [Display(Name = "Category Image")]
        public IFormFile ImageFile { get; set; }  // This will handle the image file upload
    }
}

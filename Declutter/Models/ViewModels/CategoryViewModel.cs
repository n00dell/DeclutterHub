using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DeclutterHub.Models.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Please enter a category name")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        [Display(Name = "Category Name")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Category Image")]
        public IFormFile ImageFile { get; set; }
        public string? ImageUrl { get; set; }
        [Required]
        public string CreatedBy { get; set; }

        public bool IsApproved { get; set; }
        public bool RemoveImage {  get; set; }
    }

    // Custom validation attribute for file extensions
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName);
                if (!_extensions.Contains(extension.ToLower()))
                {
                    return new ValidationResult($"Only {string.Join(", ", _extensions)} files are allowed!");
                }
            }
            return ValidationResult.Success;
        }
    }

    // Custom validation attribute for file size
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxFileSize;
        public MaxFileSizeAttribute(int maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxFileSize)
                {
                    return new ValidationResult($"Maximum allowed file size is {_maxFileSize / 1024 / 1024} MB.");
                }
            }
            return ValidationResult.Success;
        }
    }
}
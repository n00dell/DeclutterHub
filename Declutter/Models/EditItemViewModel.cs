namespace DeclutterHub.Models
{
    public class EditItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsNegotiable { get; set; }
        public string Condition { get; set; }
        public bool IsVerified { get; set; }
        public int CategoryId { get; set; }
        public bool IsSold { get; set; }
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

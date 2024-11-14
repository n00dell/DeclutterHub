using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DeclutterHub.Models
{
    public class Item
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsSold { get; set; }
        public DateTime CreatedAt { get; set; }

        public string Location {  get; set; }

  
        public string PhoneNumber { get; set; } = string.Empty;
        public string CountryCode { get; set; }
     
        public bool IsNegotiable { get; set; }

        public string Condition { get; set; }

        // Foreign key for Category
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        // Foreign key for User (who listed the item)
        public string UserId { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Image> Images { get; set; } = new List<Image>();
        public bool IsVerified {  get; set; }
    }
}

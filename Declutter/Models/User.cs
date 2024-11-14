using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DeclutterHub.Models
{
    public class User :IdentityUser
    {
       
        // Navigation property for user's listed items
        public virtual ICollection<Item>? ListedItems { get; set; }
        public string? Avatar { get; set; } // Nullable field to store avatar image URL or path
        public bool IsAdmin {  get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    }
}

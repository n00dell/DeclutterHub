using System.ComponentModel.DataAnnotations;

namespace DeclutterHub.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }

        public string Password { get; set; }

        // Navigation property for user's listed items
        public virtual ICollection<Item>? ListedItems { get; set; }




    }
}

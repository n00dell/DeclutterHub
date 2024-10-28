using System.ComponentModel.DataAnnotations;

namespace DeclutterHub.Models
{
    
        public class EditUserViewModel
        {
            public int Id { get; set; }

            [Required]
            public string Username { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }
    
}

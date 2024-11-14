using System.ComponentModel.DataAnnotations;

namespace DeclutterHub.Models
{
    
        public class EditUserViewModel
        {
            public string Id { get; set; }

            [Required]
            public string UserName { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }
    
}

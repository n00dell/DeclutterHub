using System.ComponentModel.DataAnnotations;

namespace DeclutterHub.Models
{
    public class SignUpViewModel
    {
        [Required(ErrorMessage ="Username Required")]
        public string UserName { get; set; }

        [Required(ErrorMessage ="Email Required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage ="Password Required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }

}

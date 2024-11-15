﻿using System.ComponentModel.DataAnnotations;

namespace DeclutterHub.Models.ViewModels
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

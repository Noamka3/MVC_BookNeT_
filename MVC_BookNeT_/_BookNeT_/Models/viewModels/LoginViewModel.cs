using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace _BookNeT_.Models.viewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public String Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public String Password { set; get; }

        [Required(ErrorMessage = "Please accept the terms.")]
        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
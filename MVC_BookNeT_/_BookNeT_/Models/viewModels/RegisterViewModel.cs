using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace _BookNeT_.Models.viewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "FirstName is required.")]
        public String FirstName { get; set; }

        [Required(ErrorMessage = "LastName is required.")]
        public String LastName { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\+?[0-9]{10}$", ErrorMessage = "Phone number must be between 10 digits, optionally starting with a +.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public String Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.")]
        [DataType(DataType.Password)]
        [Compare("ConfirmPassword", ErrorMessage = "Password does not match.")]
        public String Password { get; set; }
        

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public String ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Age is required.")]
        public int Age { get; set; }
    }
}
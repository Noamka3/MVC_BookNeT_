using System;
using System.ComponentModel.DataAnnotations;

namespace _BookNeT_.Models.viewModels
{
    public class VerifyEmailModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public String Email { get; set; }
    }
}

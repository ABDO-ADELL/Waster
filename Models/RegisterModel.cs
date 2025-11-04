using System.ComponentModel.DataAnnotations;

namespace Waster.Models
{
    public class RegisterModel
    {

        [Required]
        public string? FirstName { get; set; }
        [Required]
        public string? LastName { get; set; }
        [Required]
        public string? Email { get; set; }
        [Required]
        public string? Password { get; set; }
        [Required, Compare("Password", ErrorMessage = "Password and Confirm Password do not match.")]
        public string? ConfirmPassword { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        //public string? City { get; set; }
        //public string? State { get; set; }

        //public string? Role { get; set; }


    }
}

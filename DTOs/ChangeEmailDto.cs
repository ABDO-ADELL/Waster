using System.ComponentModel.DataAnnotations;

namespace Waster.DTOs
{
    public class ChangeEmailDto
    {
        [Required(ErrorMessage = "New Email is required")]
        [DataType(DataType.EmailAddress)]
        public string NewEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

    }
}

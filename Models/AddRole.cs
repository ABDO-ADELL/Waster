using System.ComponentModel.DataAnnotations;

namespace Waster.Models
{
    public class AddRole
    {
        [Required]
        public string? RoleName { get; set; }
        [Required]
        public string? UserId { get; set; }

    }
}

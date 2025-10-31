using Waster.Models.DbModels;
using Microsoft.AspNetCore.Identity;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Waster.Models
{
    public class AppUser:IdentityUser
    {
        [Required]
        public string? FirstName { get; set; }
        [Required]
        public string? LastName { get; set; }

        [NotMapped] 
        public string FullName => $"{FirstName} {LastName}";
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ProfilePictureUrl { get; set; }
   
        // Navigation properties
        public ICollection<Post> Posts { get; set; }
        public ICollection<ClaimPost> ClaimedPosts { get; set; }


        //   public string Password { get; set; }

        public ICollection<RefreshTokens> RefreshTokens { get; set; } = new List<RefreshTokens>();



    }
}

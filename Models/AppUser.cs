using Waster.Models.DbModels;
using Microsoft.AspNetCore.Identity;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

        public string? Bio {  get; set; }

        // Navigation properties
        [JsonIgnore]
        public ICollection<Post> Posts { get; set; }
        [JsonIgnore]

        public ICollection<ClaimPost> ClaimedPosts { get; set; }
        [JsonIgnore]
        public virtual ICollection<BookMark> BookMark { get; set; }
        public string BookMarkId { get; set; }
        [JsonIgnore]
        public ICollection<RefreshTokens> RefreshTokens { get; set; } = new List<RefreshTokens>();



    }
}

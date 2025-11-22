using Waster.Models;
using Waster.Models.DbModels;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public string Title { get; set; }
    [Required]
    public string Description { get; set; }
    [Required]
    public string Quantity { get; set; }
    [Required]
    public string Unit { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } // Available, Claimed, Expired
    public bool IsValid { get; set; }

    public string Category { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    [Required]
    public string PickupLocation { get; set; }
    [Required]
    public DateTime ExpiresOn { get; set; }
    public bool IsDeleted { get; set; }

    public string UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public AppUser AppUser { get; set; }
    [JsonIgnore]
    public ICollection<BookMark> BookMarks { get; set; }
    [JsonIgnore]
    public ICollection<ClaimPost> Claims { get; set; }
    [JsonIgnore]
    public ICollection<ImpactRecord> ImpactRecords { get; set; }
}

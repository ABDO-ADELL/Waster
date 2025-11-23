using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Waster.Models.DbModels;

namespace Waster.Models
{
    public class Notification
    {
            [Key]
            public Guid Id { get; set; } = Guid.NewGuid();

            // WHO gets this notification?
            public string UserId { get; set; }
            [ForeignKey(nameof(UserId))]
            public AppUser User { get; set; }

            [Required]
            [StringLength(500)]
            public string Message { get; set; }  

            [Required]
            [StringLength(50)]
            public string Type { get; set; }  // "ClaimAccepted", "ClaimRejected", "NewClaim"

            public Guid? ClaimId { get; set; }
            [ForeignKey(nameof(ClaimId))]
            public ClaimPost Claim { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public bool IsRead { get; set; } = false;
            public Guid? PostId { get; set; }
        
    }


}


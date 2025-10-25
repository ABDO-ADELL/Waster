using Waster.Models;
using Waster.Models.DbModels;

namespace Waster.Models.DbModels
{
    public class ClaimPost
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PostId { get; set; }
        public string RecipientId { get; set; }
        public DateTime ClaimedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // (Pending, Approved, Rejected, Completed)
        public Post Post { get; set; }
        public AppUser Recipient { get; set; }
        public string UserId { get; set; }
        public VolunteerAssignment? VolunteerAssignment { get; set; } // optional

    }
}




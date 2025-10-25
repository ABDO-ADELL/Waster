using Waster.Models;

namespace Waster.Models.DbModels
{
    public class VolunteerAssignment
    {
        public Guid Id { get; set; } =  Guid.NewGuid();
        public Guid? ClaimID { get; set; }
       public AppUser Volunteer { get; set; }
        public string VolunteerId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Assigned"; // (Assigned, InProgress, Completed)
        public ClaimPost? Claim { get; set; }
    }
}

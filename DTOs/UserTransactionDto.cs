using Waster.DTOs;

namespace Waster.DTOs
{
    public class UserTransactionDto
    {


        public string Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string? ProfilePicture { get; set; }
    }

    public class ClaimResponseDto
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public string Status { get; set; }
        public DateTime ClaimedAt { get; set; }

        // Post info
        public PostSummaryDto Post { get; set; }

        // Recipient info (for post owner to see)
        public UserTransactionDto Recipient { get; set; }

        // Post owner info (for recipient to see)
        public UserTransactionDto PostOwner { get; set; }
    }

    public class PostSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace Waster.DTOs
{
    public class UpdatePostDto
    {
        // All properties are nullable - only update what's provided

        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Quantity cannot exceed 50 characters")]
        public string? Quantity { get; set; }

        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        public string? Unit { get; set; }

        [StringLength(500, ErrorMessage = "Pickup location cannot exceed 500 characters")]
        public string? PickupLocation { get; set; }

        public DateTime? ExpiresOn { get; set; }

        public string? ImageData { get; set; }

        [StringLength(50)]
        public string? ImageType { get; set; }

        [StringLength(500)]
        public string? Category { get; set; }

        // NOT included (system managed or immutable):
        // - Id (in URL path)
        // - UserId (can't change owner)
        // - Status (should have separate endpoint)
        // - IsDeleted (should have separate endpoint)
        // - IsValid (system managed)
        // - Created (never changes)
        // - Updated (auto-set by system)

    }
}

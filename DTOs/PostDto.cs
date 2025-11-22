using System.ComponentModel.DataAnnotations;

public class PostDto
{
    // DTO for creating a new post
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [StringLength(50)]
        public string Quantity { get; set; }

        [Required(ErrorMessage = "Unit is required")]
        [StringLength(50)]
        public string Unit { get; set; }


        // Optional image data
      //  public byte[]? ImageData { get; set; }

        [StringLength(50)]
        public string? ImageType { get; set; }

        [StringLength(500)]
        public string PickupLocation { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        public DateTime ExpiresOn { get; set; }
    [Required(ErrorMessage = "Category is required")]
    [StringLength(100)]
    public string Category { get; set; }
    public string? ImageData { get; set; } 

}
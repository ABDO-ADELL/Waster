namespace Waster.DTOs
{
    public class ClaimInfoDto
    {
        public Guid Id { get; set; }
        public DateTime ClaimedAt { get; set; }
        public string Status { get; set; }
        public string UserName { get; set; }
    }
}

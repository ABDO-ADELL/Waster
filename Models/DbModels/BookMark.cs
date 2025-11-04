namespace Waster.Models.DbModels
{
    public class BookMark
    {
        public string Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PostId { get; set; }

        public virtual AppUser User { get; set; }
        public virtual Post Post { get; set; }
    }
}

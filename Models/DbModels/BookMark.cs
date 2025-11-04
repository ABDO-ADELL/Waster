namespace Waster.Models.DbModels
{
    public class BookMark
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }  
        public Guid PostId { get; set; }

        public virtual AppUser User { get; set; }
        public virtual Post Post { get; set; }
    }
}

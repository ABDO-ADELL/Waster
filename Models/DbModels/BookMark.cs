using System.Text.Json.Serialization;

namespace Waster.Models.DbModels
{
    public class BookMark
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        public Guid PostId { get; set; }
        [JsonIgnore]
        public virtual AppUser User { get; set; }
        [JsonIgnore]
        public virtual Post Post { get; set; }
    }
}

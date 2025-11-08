using System.Text.Json.Serialization;
using Waster.Models;

namespace Waster.Models.DbModels
{
    public class ImpactRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        [JsonIgnore]
        public AppUser User { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public Post Post { get; set; }
        [JsonIgnore]
        public Guid PostId { get; set; }
        public int MealsProvided { get; set; }
        public decimal CO2Reduced { get; set; }
        public double WasteDivertedKg { get; set; }
    }
}

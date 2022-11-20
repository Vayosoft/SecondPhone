using Vayosoft.Commons.Entities;

namespace EmulatorHub.Domain.Entities
{
    public class TestEntity : EntityBase<long>
    {
        public DateTime Timestamp { get; set; }
        public string Name { get; set; } = null!;
        public string? Alias { get; set; }
        public string DisplayName { get; set; } = null!;
        public DateOnly RegisteredDate { get; set; }
        public double Double { get; set; }

        public long ProviderId { get; set; }
        public bool SoftDeleted { get; set; }
    }
}

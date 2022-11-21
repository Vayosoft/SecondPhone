using Vayosoft.Commons.Entities;

namespace EmulatorHub.Domain.Entities
{
    public class TestEntity : EntityBase<long>, ISoftDelete
    {
        public DateTime Timestamp { get; set; }
        public string Name { get; set; } = null!;
        public string? Alias { get; set; }
        public string DisplayName { get; set; } = null!;
        public DateOnly RegisteredDate { get; set; }
        public double Double { get; set; }
        public TestEnum Enum { get; set; }

        public byte[] ChangeCheck { get; set; } = null!; // concurrency row_version

        public long ProviderId { get; set; }
        public bool SoftDeleted { get; set; }
    }

    public enum TestEnum
    {
        Undefined = 0,
        Ok,
        Error
    }
}

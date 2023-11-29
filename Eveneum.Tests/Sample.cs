using NodaTime;

namespace Eveneum.Tests
{
    public class SampleMetadata
    {
        public bool Property { get; set; }
        public NestedContent Nested { get; set; }
    }

    public struct SampleEvent
    {
        public int Version { get; set; }
        public decimal Number { get; set; }
        public SampleEnum Enum { get; set; }
        public LocalDate LocalDate{ get; set; }
        public LocalDateTime LocalDateTime { get; set; }
        public NestedContent Nested { get; set; }
    }

    public enum SampleEnum
    {
        Value1,
        Value2,
        Value3
    }

    public struct SampleSnapshot
    {
        public int Version { get; set; }
        public double Number { get; set; }
        public NestedContent Nested { get; set; }
    }

    public struct NestedContent
    {
        public string Content;
    }
}

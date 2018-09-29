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
        public NestedContent Nested { get; set; }
    }

    public struct NestedContent
    {
        public string Content;
    }
}

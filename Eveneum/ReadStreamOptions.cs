namespace Eveneum
{
    public class ReadStreamOptions
    {
        public ulong? FromVersion { get; set; } = null;
        public ulong? ToVersion { get; set; } = null;
        public bool IgnoreSnapshots { get; set; } = false;
        public int? MaxItemCount { get; set; } = null;
    }
}

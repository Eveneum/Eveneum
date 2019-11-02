namespace Eveneum.StoredProcedures
{
    public class BulkDeleteResponse
    {
        public uint Deleted { get; set; }
        public bool Continuation { get; set; }
    }
}

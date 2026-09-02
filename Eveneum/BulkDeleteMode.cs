namespace Eveneum
{
    public enum BulkDeleteMode
    {
        StoredProcedure = 1, // uses the BulkDelete stored procedure created by Initialize()
        TransactionalBatch = 2, // uses transactional batches, no stored procedures required (compatible with the Linux-based (vNext) Cosmos DB emulator)
    }
}

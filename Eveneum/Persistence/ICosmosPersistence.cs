using Eveneum.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using System.Threading;
using System.Threading.Tasks;

namespace Eveneum.Persistence
{
    /// <summary>
    /// Abstraction for CosmosDB persistence operations used by EventStore.
    /// This interface isolates the EventStore from direct CosmosDB SDK and JSON serializer dependencies.
    /// </summary>
    public interface ICosmosPersistence
    {
        /// <summary>
        /// Initialize the persistence layer (e.g., create stored procedures).
        /// </summary>
        Task Initialize(CancellationToken cancellationToken = default);

        /// <summary>
        /// Query documents with a string-based SQL query.
        /// </summary>
        ICosmosFeedIterator<IEveneumDocument> GetItemQueryIterator(string queryText, string partitionKey, int? maxItemCount = null);

        /// <summary>
        /// Query documents with a QueryDefinition.
        /// </summary>
        ICosmosFeedIterator<IEveneumDocument> GetItemQueryIterator(QueryDefinition queryDefinition, int? maxItemCount = null);

        /// <summary>
        /// Read a single document by id and partition key.
        /// </summary>
        Task<CosmosItemResponse<IEveneumDocument>> ReadItemAsync(
            string id,
            string partitionKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a transactional batch for atomic operations.
        /// </summary>
        TransactionalBatch CreateTransactionalBatch(string partitionKey);

        /// <summary>
        /// Upsert (insert or replace) a document.
        /// </summary>
        Task<CosmosItemResponse<IEveneumDocument>> UpsertItemAsync(
            IEveneumDocument document,
            string partitionKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Replace an existing document.
        /// </summary>
        Task<CosmosItemResponse<IEveneumDocument>> ReplaceItemAsync(
            IEveneumDocument document,
            string id,
            string partitionKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes documents in a stream that match a query, in batches, with optional soft-delete behavior.
        /// </summary>
        /// <remarks>
        /// When soft deletion is disabled, the TTL value may be ignored by the implementation.
        /// </remarks>
        Task<DeleteResponse> DeleteItems(
            string streamId, 
            string query, 
            bool softDelete, 
            double ttl, 
            byte batchSize, 
            int? maxItemCount = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute a stored procedure.
        /// </summary>
        Task<StoredProcedureExecuteResponse<T>> ExecuteStoredProcedureAsync<T>(
            string storedProcedureId,
            string partitionKey,
            object[] parameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Create or update a stored procedure.
        /// </summary>
        Task CreateOrUpdateStoredProcedureAsync(
            string procedureId,
            string procedureBody,
            CancellationToken cancellationToken = default);
    }
}

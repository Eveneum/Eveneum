using Eveneum.Documents;
using Eveneum.StoredProcedures;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Eveneum.Persistence
{
    public class CosmosPersistence<TDocument> : ICosmosPersistence
        where TDocument : class, IEveneumDocument
    {
        protected readonly Container Container;
        protected readonly BulkDeleteMode BulkDeleteMode;

        private const string BulkDeleteStoredProc = "Eveneum.BulkDelete";

        public CosmosPersistence(CosmosClient cosmosClient, string databaseName, string containerName, BulkDeleteMode bulkDeleteMode = BulkDeleteMode.StoredProcedure)
        {
            if (cosmosClient == null)
                throw new ArgumentNullException(nameof(cosmosClient));

            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName));

            if (string.IsNullOrEmpty(containerName))
                throw new ArgumentNullException(nameof(containerName));

            var database = cosmosClient.GetDatabase(databaseName);
            this.Container = database.GetContainer(containerName);

            this.BulkDeleteMode = bulkDeleteMode;
        }

        public virtual async Task Initialize(CancellationToken cancellationToken = default)
        {
            if (this.BulkDeleteMode == BulkDeleteMode.StoredProcedure)
                await CreateOrUpdateStoredProcedureAsync(BulkDeleteStoredProc, "BulkDelete", cancellationToken);
        }

        public async Task CreateOrUpdateStoredProcedureAsync(
            string procedureId,
            string procedureFileName,
            CancellationToken cancellationToken = default)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                typeof(EventStore),
                $"StoredProcedures.{procedureFileName}.js");
            
            if (stream == null)
                throw new InvalidOperationException($"Could not find embedded resource for stored procedure: {procedureFileName}");

            using var reader = new StreamReader(stream);

            var properties = new StoredProcedureProperties
            {
                Id = procedureId,
                Body = await reader.ReadToEndAsync()
            };

            try
            {
                await Container.Scripts.ReadStoredProcedureAsync(procedureId, cancellationToken: cancellationToken);
                await Container.Scripts.ReplaceStoredProcedureAsync(properties, cancellationToken: cancellationToken);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await Container.Scripts.CreateStoredProcedureAsync(properties, cancellationToken: cancellationToken);
            }
        }

        public virtual async Task<CosmosItemResponse<IEveneumDocument>> UpsertItemAsync(
            IEveneumDocument document,
            string partitionKey,
            CancellationToken cancellationToken = default)
        {
            var result = await Container.UpsertItemAsync(
                document,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);

            return new CosmosItemResponse<IEveneumDocument>(result.Resource, result.RequestCharge);
        }

        public virtual async Task<CosmosItemResponse<IEveneumDocument>> ReplaceItemAsync(
            IEveneumDocument document,
            string id,
            string partitionKey,
            CancellationToken cancellationToken = default)
        {
            var result = await Container.ReplaceItemAsync(
                document,
                id,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);

            return new CosmosItemResponse<IEveneumDocument>(result.Resource, result.RequestCharge);
        }

        public virtual async Task<StoredProcedureExecuteResponse<T>> ExecuteStoredProcedureAsync<T>(
            string storedProcedureId,
            string partitionKey,
            object[] parameters,
            CancellationToken cancellationToken = default)
        {
            return await Container.Scripts.ExecuteStoredProcedureAsync<T>(
                storedProcedureId,
                new PartitionKey(partitionKey),
                parameters,
                cancellationToken: cancellationToken);
        }

        public virtual TransactionalBatch CreateTransactionalBatch(string partitionKey)
        {
            return Container.CreateTransactionalBatch(new PartitionKey(partitionKey));
        }

        public virtual ICosmosFeedIterator<IEveneumDocument> GetItemQueryIterator(string queryText, string partitionKey, int? maxItemCount = null)
        {
            var requestOptions = new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(partitionKey),
                MaxItemCount = maxItemCount
            };

            return new CosmosFeedIterator<IEveneumDocument, TDocument>(Container.GetItemQueryIterator<TDocument>(queryText, requestOptions: requestOptions));
        }

        public virtual ICosmosFeedIterator<IEveneumDocument> GetItemQueryIterator(QueryDefinition queryDefinition, int? maxItemCount = null)
        {
            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = maxItemCount
            };

            return new CosmosFeedIterator<IEveneumDocument, TDocument>(Container.GetItemQueryIterator<TDocument>(queryDefinition, requestOptions: requestOptions));
        }

        public virtual async Task<CosmosItemResponse<IEveneumDocument>> ReadItemAsync(string id, string partitionKey, CancellationToken cancellationToken = default)
        {
            var result = await Container.ReadItemAsync<IEveneumDocument>(
                id,
                new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);

            return new CosmosItemResponse<IEveneumDocument>(result.Resource, result.RequestCharge);
        }

        public Task<DeleteResponse> DeleteItems(string streamId, string query, bool softDelete, double ttl, byte batchSize, int? maxItemCount = null, CancellationToken cancellationToken = default) =>
            this.BulkDeleteMode == BulkDeleteMode.TransactionalBatch
                ? this.BulkDeleteDocumentsUsingTransactionalBatch(streamId, query, softDelete, ttl, batchSize, maxItemCount, cancellationToken)
                : this.BulkDeleteDocumentsUsingStoredProcedure(streamId, query, softDelete, ttl, cancellationToken);

        private async Task<DeleteResponse> BulkDeleteDocumentsUsingStoredProcedure(string streamId, string query, bool softDelete, double ttl, CancellationToken cancellationToken = default)
        {
            double requestCharge = 0;
            ulong deletedDocuments = 0;
            StoredProcedureExecuteResponse<BulkDeleteResponse> response;

            do
            {
                response = await this.ExecuteStoredProcedureAsync<BulkDeleteResponse>(
                    BulkDeleteStoredProc,
                    streamId,
                    [query, softDelete, ttl],
                    cancellationToken);

                requestCharge += response.RequestCharge;
                deletedDocuments += response.Resource.Deleted;
            }
            while (response.Resource.Continuation);

            return new DeleteResponse(deletedDocuments, requestCharge);
        }

        private async Task<DeleteResponse> BulkDeleteDocumentsUsingTransactionalBatch(string streamId, string query, bool softDelete, double ttl, byte batchSize, int? maxItemCount = null, CancellationToken cancellationToken = default)
        {
            double requestCharge = 0;
            ulong deletedDocuments = 0;
            List<IEveneumDocument> documents;

            do
            {
                documents = [];

                using (var iterator = this.GetItemQueryIterator(query, streamId, maxItemCount))
                {
                    while (iterator.HasMoreResults && documents.Count == 0)
                    {
                        var page = await iterator.ReadNextAsync(cancellationToken);

                        requestCharge += page.RequestCharge;
                        documents.AddRange(page);
                    }
                }

                foreach (var batch in documents.Batch(batchSize))
                {
                    if (!batch.Any())
                        continue;

                    var transaction = this.CreateTransactionalBatch(streamId);

                    foreach (var document in batch)
                    {
                        if (softDelete)
                        {
                            document.Deleted = true;

                            if (ttl > 0)
                                document.TimeToLive = (int)ttl;

                            transaction.ReplaceItem(document.Id, document, new TransactionalBatchItemRequestOptions { IfMatchEtag = document.ETag });
                        }
                        else
                            transaction.DeleteItem(document.Id, new TransactionalBatchItemRequestOptions { IfMatchEtag = document.ETag });
                    }

                    using var response = await transaction.ExecuteAsync(cancellationToken);
                    requestCharge += response.RequestCharge;

                    if (!response.IsSuccessStatusCode)
                        throw new WriteException(streamId, requestCharge, response.ErrorMessage, response.StatusCode);

                    deletedDocuments += (ulong)batch.Count();
                }
            }
            while (documents.Count > 0);

            return new DeleteResponse(deletedDocuments, requestCharge);
        }
    }
}

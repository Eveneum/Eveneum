using Microsoft.Azure.Cosmos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eveneum.Persistence
{
    public interface ICosmosFeedIterator<T> : IDisposable
    {
        bool HasMoreResults { get; }

        Task<ICosmosFeedResponse<T>> ReadNextAsync(CancellationToken cancellationToken = default);
    }

    public class CosmosFeedIterator<T, TDocument> : ICosmosFeedIterator<T>
        where T : class
        where TDocument : class, T
    {
        private readonly FeedIterator<TDocument> FeedIterator;

        public CosmosFeedIterator(FeedIterator<TDocument> feedIterator)
        {
            this.FeedIterator = feedIterator ?? throw new ArgumentNullException(nameof(feedIterator));
        }

        public bool HasMoreResults => this.FeedIterator.HasMoreResults;

        public void Dispose()
        {
            this.FeedIterator.Dispose();
        }

        public async Task<ICosmosFeedResponse<T>> ReadNextAsync(CancellationToken cancellationToken = default)
        {
            return new CosmosFeedResponse<T, TDocument>(await this.FeedIterator.ReadNextAsync(cancellationToken));
        }
    }

    public interface ICosmosFeedResponse<T> : IEnumerable<T>
    {
        double RequestCharge { get; }
    }

    public class CosmosFeedResponse<T, TDocument> : ICosmosFeedResponse<T>
        where T : class
        where TDocument : class, T
    {
        private readonly FeedResponse<TDocument> FeedResponse;

        internal CosmosFeedResponse(FeedResponse<TDocument> feedResponse)
        {
            this.FeedResponse = feedResponse ?? throw new ArgumentNullException(nameof(feedResponse));
        }

        public double RequestCharge => this.FeedResponse.RequestCharge;

        public IEnumerator<T> GetEnumerator() => this.FeedResponse.Resource.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    public class CosmosItemResponse<T>
    {
        public CosmosItemResponse(T resource, double requestCharge)
        {
            Resource = resource;
            RequestCharge = requestCharge;
        }

        public T Resource { get; private set; }
        public double RequestCharge { get; private set; }
    }
}

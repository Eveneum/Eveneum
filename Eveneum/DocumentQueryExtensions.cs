using Eveneum.Documents;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eveneum
{
    static class DocumentQueryExtensions
    {
        public static async Task<IReadOnlyCollection<EveneumDocument>> All(this IDocumentQuery<Document> query)
        {
            var documents = new List<EveneumDocument>();

            while (query.HasMoreResults)
            {
                var page = await query.ExecuteNextAsync<Document>();

                documents.AddRange(page.Select(EveneumDocument.Parse));
            }

            return documents;
        }
    }
}

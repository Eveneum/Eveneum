function BulkDelete(query, softDelete) {
    var collection = getContext().getCollection();
    var collectionLink = collection.getSelfLink();
    var response = getContext().getResponse();
    var responseBody = {
        deleted: 0,
        continuation: true
    };

    // Validate input.
    if (!query) throw new Error("The query is undefined or null.");

    tryQueryAndDelete();

    // Recursively runs the query w/ support for continuation tokens.
    // Calls tryDelete(documents) as soon as the query returns documents.
    function tryQueryAndDelete(continuation) {
        var requestOptions = { continuation: continuation };

        var isAccepted = collection.queryDocuments(collectionLink, query, requestOptions, function (err, retrievedDocs, responseOptions) {
            if (err) throw err;

            if (retrievedDocs.length > 0) {
                // Begin deleting documents as soon as documents are returned form the query results.
                // tryDelete() resumes querying after deleting; no need to page through continuation tokens.
                //  - this is to prioritize writes over reads given timeout constraints.
                tryDelete(retrievedDocs);
            } else if (responseOptions.continuation) {
                // Else if the query came back empty, but with a continuation token; repeat the query w/ the token.
                tryQueryAndDelete(responseOptions.continuation);
            } else {
                // Else if there are no more documents and no continuation token - we are finished deleting documents.
                responseBody.continuation = false;
                response.setBody(responseBody);
            }
        });

        // If we hit execution bounds - return continuation: true.
        if (!isAccepted) {
            response.setBody(responseBody);
        }
    }

    // Recursively deletes documents passed in as an array argument.
    // Attempts to query for more on empty array.
    function tryDelete(documents) {
        if (documents.length > 0) {
            var requestOptions = { etag: documents[0]._etag };

            var isAccepted = true;

            if (softDelete) {
                documents[0].Deleted = true;

                isAccepted = collection.replaceDocument(documents[0]._self, documents[0], requestOptions, function (err, responseOptions) {
                    if (err) throw err;

                    responseBody.deleted++;
                    documents.shift();
                    // Delete the next document in the array.
                    tryDelete(documents);
                });
            }
            else {
                isAccepted = collection.deleteDocument(documents[0]._self, requestOptions, function (err, responseOptions) {
                    if (err) throw err;

                    responseBody.deleted++;
                    documents.shift();
                    // Delete the next document in the array.
                    tryDelete(documents);
                });
            }

            // If we hit execution bounds - return continuation: true.
            if (!isAccepted) {
                response.setBody(responseBody);
            }
        } else {
            // If the document array is empty, query for more documents.
            tryQueryAndDelete();
        }
    }
}
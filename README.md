[![Build Status](https://dev.azure.com/jkonecki/Eveneum/_apis/build/status/Continuous%20Integration)](https://dev.azure.com/jkonecki/Eveneum/_build/latest?definitionId=2)

# Eveneum

Eveneum is a simple, developer-friendly Event Store with snaphots backed by Azure CosmosDB.



## Project Goals

The aim of the project is to provide a straightforward implementation of Event Store by utilising the features of CosmosDB. The library will benefit from autamatic indexing, replication and scalability offered by CosmosDB. 

 - Ability to store and read stream of events in a single method call. The library will handle retries and batching,
 - Ability to store and read snapshots with the support of reading a snapshot and only consecutive events,
 - Ability to customize the schema of documents stored in CosmosDB to allow for rich querying capabilities,
 - Built-in optimistic concurrency checks,
 - Support for both partitioned and non-partitioned collections in CosmosDB,
 - CosmosDB ChangeFeed-friendly design to enable building independent projections using Change Feed.

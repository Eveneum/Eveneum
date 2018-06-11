# Eveneum

Eveneum will be a simple, developer-friendly Event Store with snaphots backed by Azure CosmosDB Table API.

## Project Goals

The aim of the project is to provide a straightforward implementation of Event Store by utilising the features of CosmosDB. The library will benefit from autamatic indexing, replication and scalability offered by CosmosDB. 

 - Ability to store and read stream of events in a single method call. The library will handle retries and batching,
 - Ability to store and read snapshots with the support of reading a snapshot and only consecutive events,
 - Ability to customize the schema of documents stored in CosmosDB.

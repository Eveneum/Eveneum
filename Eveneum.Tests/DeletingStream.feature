Feature: Deleting stream
	Deleting an existing stream soft-deletes all documents so the changes are present in Cosmos DB Change Feed

Scenario Outline: Deleting stream with no events
	Given an event store backed by <partitioned> collection
	And an existing stream P with 0 events
	And an existing stream S with 0 events
	When I delete stream S in expected version 0
	Then the header is soft-deleted
	And stream P is not soft-deleted
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
	
Scenario Outline: Deleting stream with some events
	Given an event store backed by <partitioned> collection
	And an existing stream P with 10 events
	And an existing stream S with 5 events
	When I delete stream S in expected version 5
	Then the header is soft-deleted
	And all events are soft-deleted
	And stream P is not soft-deleted
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

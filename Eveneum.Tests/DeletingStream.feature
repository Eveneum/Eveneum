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
	
Scenario Outline: Deleting stream with some events and snapshots
	Given an event store backed by <partitioned> collection
	And an existing stream P with 10 events
	And an existing snapshot for version 5
	And an existing stream S with 5 events
	And an existing snapshot for version 1
	And an existing snapshot for version 3
	When I delete stream S in expected version 5
	Then the header is soft-deleted
	And all events are soft-deleted
	And all snapshots are soft-deleted
	And stream P is not soft-deleted
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
		
Scenario Outline: Hard-deleting stream with no events
	Given an event store backed by <partitioned> collection
	And hard-delete mode
	And an existing stream P with 0 events
	And an existing stream S with 0 events
	When I delete stream S in expected version 0
	Then the header is hard-deleted
	And stream P is not hard-deleted
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
	
Scenario Outline: Hard-deleting stream with some events
	Given an event store backed by <partitioned> collection
	And hard-delete mode
	And an existing stream P with 10 events
	And an existing stream S with 5 events
	When I delete stream S in expected version 5
	Then the header is hard-deleted
	And all events are hard-deleted
	And stream P is not hard-deleted
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
	
Scenario Outline: Hard-deleting stream with some events and snapshots
	Given an event store backed by <partitioned> collection
	And hard-delete mode
	And an existing stream P with 10 events
	And an existing snapshot for version 5
	And an existing stream S with 5 events
	And an existing snapshot for version 1
	And an existing snapshot for version 3
	When I delete stream S in expected version 5
	Then the header is hard-deleted
	And all events are hard-deleted
	And all snapshots are hard-deleted
	And stream P is not hard-deleted
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
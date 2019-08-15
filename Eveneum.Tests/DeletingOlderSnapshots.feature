Feature: Deleting older snapshots
	Older snapshots can be optionally deleted when creating a new snapshot

Scenario: Deleting older snapshots
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 3
	And an existing snapshot for version 5
	And an existing snapshot for version 7
	When I create snapshot for stream S in version 10 and delete older snapshots
	Then the snapshots older than 10 are soft-deleted
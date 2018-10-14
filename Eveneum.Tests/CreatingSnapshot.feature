Feature: Creating snapshot

Scenario Outline: Creating snapshot
	Given an event store backed by <partitioned> collection
	And an existing stream S with 10 events
	When I create snapshot for stream S in version 5
	Then the snapshot for version 5 is persisted
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

Feature: Deleting already deleted stream
	Deleting a stream that has already been deleted fails with StreamNotFound exception

@ExpectException
Scenario Outline: Deleting already deleted stream
	Given an event store backed by <partitioned> collection
	And a deleted stream S with 10 events
	When I delete stream S in expected version 10
	Then the action fails as stream S doesn't exist
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

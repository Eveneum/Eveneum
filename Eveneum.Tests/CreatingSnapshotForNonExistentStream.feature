Feature: Creating snapshot for non-existent stream
	Creating a snapshot for a stream that doesn't exist fails with StreamNotFound exception

@ExpectException
Scenario Outline: Creating snapshot for non-existent stream
	Given an event store backed by <partitioned> collection
	And an existing stream S with 10 events
	When I create snapshot for stream X in version 5
	Then the action fails as stream X doesn't exist
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
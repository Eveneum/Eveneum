Feature: Deleting stream with non-matching expected version
	The action fails with OptimisticConcurrency exception when trying to delete an existing stream and stream actual version doesn't match the expected one

@ExpectException
Scenario Outline: Deleting stream with no events with non-matching expected version fails
	Given an event store backed by <partitioned> collection
	And an existing stream S with 0 events
	When I delete stream S in expected version 5
	Then the action fails as expected version 5 doesn't match the current version 0 of stream S
	And stream S is not soft-deleted
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
		
@ExpectException
Scenario Outline: Deleting stream with some events with non-matching expected version fails
	Given an event store backed by <partitioned> collection
	And an existing stream S with 10 events
	When I delete stream S in expected version 5
	Then the action fails as expected version 5 doesn't match the current version 10 of stream S
	And stream S is not soft-deleted
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
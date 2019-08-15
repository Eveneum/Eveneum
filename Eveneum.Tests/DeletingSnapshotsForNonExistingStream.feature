Feature: Deleting snapshots for non-existing stream
	Deleting fnapshots from a stream that doesn't exist fails with StreamNotFound exception

@ExpectException
Scenario: Deleting some snapshots
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 3
	And an existing snapshot for version 7
	When I delete snapshots older than version 10 from stream X
	Then the action fails as stream X doesn't exist
	And stream S is not soft-deleted
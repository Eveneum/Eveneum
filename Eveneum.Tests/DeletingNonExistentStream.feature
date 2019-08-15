Feature: Deleting non-existent stream
	Deleting a stream that doesn't exist fails with StreamNotFound exception

@ExpectException
Scenario: Deleting non-existent stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I delete stream X in expected version 10
	Then the action fails as stream X doesn't exist
	And stream S is not soft-deleted
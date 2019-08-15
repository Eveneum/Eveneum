Feature: Appending to non-existent stream
	Appending to a stream that doesn't exist fails with StreamNotFound exception

@ExpectException
Scenario: Appending to non-existent stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I append 5 events to stream X in expected version 10
	Then the action fails as stream X doesn't exist
	And no events are appended
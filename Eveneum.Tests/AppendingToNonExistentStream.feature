Feature: Appending to non-existent stream
	Appending to a stream that doesn't exist fails with StreamNotFound exception

@ExpectException
Scenario: Appending to non-existent stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I append 5 events to stream X in expected version 10
	Then the action fails as stream X doesn't exist
	And no events are appended
	And request charge is reported
	
@ExpectException
Scenario: Appending to soft-deleted stream
	Given an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I append 5 events to stream S in expected version 10
	Then the action fails as stream S doesn't exist
	And the action fails as stream S has been deleted
	And no events are appended	
	And request charge is reported

@ExpectException
Scenario: Appending to hard-deleted stream
	Given hard-delete mode
	And an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I append 5 events to stream S in expected version 10
	Then the action fails as stream S doesn't exist
	And no events are appended
	And request charge is reported
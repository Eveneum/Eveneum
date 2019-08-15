Feature: Appending to existing stream with non-matching expected version
	The action fails with OptimisticConcurrency exception when trying to append new events to an existing stream and stream actual version doesn't match the expected one

@ExpectException
Scenario: Appending to stream with no events with non-matching expected version fails
	Given an event store backed by partitioned collection
	And an existing stream S with 0 events
	When I append 10 events to stream S in expected version 6
	Then the action fails as expected version 6 doesn't match the current version 0 of stream S
	And no events are appended

@ExpectException
Scenario: Appending to stream with metadata and no events with non-matching expected version fails
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 0 events
	When I append 10 events to stream S in expected version 6
	Then the action fails as expected version 6 doesn't match the current version 0 of stream S
	And no events are appended

@ExpectException
Scenario: Appending to stream with some events with non-matching expected version fails
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I append 5 events to stream S in expected version 6
	Then the action fails as expected version 6 doesn't match the current version 10 of stream S
	And no events are appended
		
@ExpectException
Scenario: Appending to stream with metadata and some events with non-matching expected version fails
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 20 events
	When I append 10 events to stream S in expected version 6
	Then the action fails as expected version 6 doesn't match the current version 20 of stream S
	And no events are appended
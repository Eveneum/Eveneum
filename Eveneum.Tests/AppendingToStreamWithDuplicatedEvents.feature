Feature: Appending to stream with duplicated events
Action fails with EventAlreadyExistsException when trying to append event with version that already exists

@ExpectException
Scenario: Appending to stream with duplicated event fails
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I append events with version 10 to stream S in expected version 10
	Then the action fails as event with version 10 already exists in stream S
	And no events are appended
	And request charge is reported

@ExpectException
Scenario: Appending to stream with duplicated events fails
	The first event is reported
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I append events with version 5,6,7,8 to stream S in expected version 10
	Then the action fails as event with version 5 already exists in stream S
	And no events are appended
	And request charge is reported

@ExpectException
Scenario: Appending to stream with duplicated event in consecurive batch fails
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I append 175 events and events with version 7 to stream S in expected version 10
	Then the action fails as event with version 7 already exists in stream S
	And first 99 events are appended
	And request charge is reported
Feature: Writing events without body
	Writing events fails with ArgumentException before anything is persisted when any of the events has no body

Scenario: Creating new stream with an event without body in a later batch fails
	Given a batch size of 2
	And an event store
	When I write a new stream S with 3 events where event at index 1 has no body
	Then stream S is not persisted
	And the action fails as event at index 1 has no body

Scenario: Appending to stream with an event without body in a later batch fails
	Given a batch size of 2
	And an event store
	And an existing stream S with 5 events
	When I append 3 events to stream S in expected version 5 where event at index 1 has no body
	Then no events are appended
	And the header version 5 with no metadata is persisted
	And the action fails as event at index 1 has no body

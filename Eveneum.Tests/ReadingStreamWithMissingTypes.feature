Feature: Reading stream with missing types
	Reading a stream with an unresolvable event or metadata type fails with a StreamDeserialization exception, unless missing types are ignored

@ExpectException
Scenario: Reading stream with an unresolvable event type fails with a StreamDeserialization exception
	Given a type provider returning unresolvable type identifiers
	And an event store
	And an existing stream S with 5 events
	When I read stream S
	Then the action fails to read stream S because a type wasn't found
	And request charge is reported

Scenario: Reading stream with an unresolvable event type succeeds when missing types are ignored
	Given an event store that ignores missing types
	And a type provider returning unresolvable type identifiers
	And an event store
	And an existing stream S with 5 events
	When I read stream S
	Then the stream S in version 5 is returned
	And all events are returned without body or metadata

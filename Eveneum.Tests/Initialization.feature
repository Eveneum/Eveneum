Feature: Initialization
	Verifying EventStore initialization


@ExpectException
Scenario: Reading stream
	Given an uninitialized event store backed by partitioned collection
	When I read stream S
	Then the action fails as event store is not initialized

@ExpectException
Scenario: Appending to stream
	Given an uninitialized event store backed by partitioned collection
	When I append 5 events to stream S in expected version 10
	Then the action fails as event store is not initialized

@ExpectException
Scenario: Deleting stream
	Given an uninitialized event store backed by partitioned collection
	When I delete stream S in expected version 5
	Then the action fails as event store is not initialized

@ExpectException
Scenario: Creating snapshot
	Given an uninitialized event store backed by partitioned collection
	When I create snapshot for stream S in version 5
	Then the action fails as event store is not initialized

@ExpectException
Scenario: Deleting snapshots
	Given an uninitialized event store backed by partitioned collection
	When I delete snapshots older than version 5 from stream S
	Then the action fails as event store is not initialized

@ExpectException
Scenario: Advanced - Loading all events
	Given an uninitialized event store backed by partitioned collection
	When I load all events
	Then the action fails as event store is not initialized

@ExpectException
Scenario: Advanced - Querying events
	Given an uninitialized event store backed by partitioned collection
	When I load events using query SELECT * from c WHERE c.StreamId = 'B' or c.StreamId = 'C'
	Then the action fails as event store is not initialized

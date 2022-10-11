Feature: Deleting stream
	Deleting an existing stream soft-deletes all documents so the changes are present in Cosmos DB Change Feed

Scenario: Deleting stream with no events
	Given an event store backed by partitioned collection
	And an existing stream P with 0 events
	And an existing stream S with 0 events
	When I delete stream S in expected version 0
	Then the header is soft-deleted
	And stream P is not soft-deleted
	And request charge is reported
	And 1 deleted documents are reported
	
Scenario: Deleting stream with some events
	Given an event store backed by partitioned collection
	And an existing stream P with 10 events
	And an existing stream S with 5 events
	When I delete stream S in expected version 5
	Then the header is soft-deleted
	And all events are soft-deleted
	And stream P is not soft-deleted
	And request charge is reported
	And 6 deleted documents are reported
	
Scenario: Deleting stream with some events and snapshots
	Given an event store backed by partitioned collection
	And an existing stream P with 10 events
	And an existing snapshot for version 5
	And an existing stream S with 5 events
	And an existing snapshot for version 1
	And an existing snapshot for version 3
	When I delete stream S in expected version 5
	Then the header is soft-deleted
	And all events are soft-deleted
	And all snapshots are soft-deleted
	And stream P is not soft-deleted
	And request charge is reported
	And 8 deleted documents are reported
	
Scenario: Hard-deleting stream with no events
	Given hard-delete mode
	And an event store backed by partitioned collection
	And an existing stream P with 0 events
	And an existing stream S with 0 events
	When I delete stream S in expected version 0
	Then the header is hard-deleted
	And stream P is not hard-deleted
	And request charge is reported
	And 1 deleted documents are reported
	
Scenario: Hard-deleting stream with some events
	Given hard-delete mode
	And an event store backed by partitioned collection
	And an existing stream P with 10 events
	And an existing stream S with 5 events
	When I delete stream S in expected version 5
	Then the header is hard-deleted
	And all events are hard-deleted
	And stream P is not hard-deleted
	And request charge is reported
	And 6 deleted documents are reported	

Scenario: Hard-deleting stream with some events and snapshots
	Given hard-delete mode
	And an event store backed by partitioned collection
	And an existing stream P with 10 events
	And an existing snapshot for version 5
	And an existing stream S with 5 events
	And an existing snapshot for version 1
	And an existing snapshot for version 3
	When I delete stream S in expected version 5
	Then the header is hard-deleted
	And all events are hard-deleted
	And all snapshots are hard-deleted
	And stream P is not hard-deleted
	And request charge is reported
	And 8 deleted documents are reported
	
Scenario: Hard-deleting stream with many events and snapshots
	Given hard-delete mode
	And an event store backed by partitioned collection
	And an existing stream P with 10 events
	And an existing stream S with 10000 events
	And an existing snapshot for version 100
	And an existing snapshot for version 200
	And an existing snapshot for version 300
	And an existing snapshot for version 400
	And an existing snapshot for version 500
	And an existing snapshot for version 600
	And an existing snapshot for version 700
	And an existing snapshot for version 800
	And an existing snapshot for version 900
	And an existing snapshot for version 1000
	When I delete stream S in expected version 10000
	Then the header is hard-deleted
	And all events are hard-deleted
	And all snapshots are hard-deleted
	And stream P is not hard-deleted
	And request charge is reported
	And 10011 deleted documents are reported

Scenario: TTl-delete stream with events and snapshots
	Given ttl-delete mode with 10 seconds as ttl
	And an event store backed by partitioned collection
	And an existing stream Z with 13 events
	And an existing stream P with 10 events
	And an existing snapshot for version 5
	When I delete stream P in expected version 10
	Then the header is soft-deleted with TTL set to 10 seconds
	And all events are soft-deleted with TTL set to 10 seconds
	And all snapshots are soft-deleted with TTL set to 10 seconds
	And stream Z is not soft-deleted
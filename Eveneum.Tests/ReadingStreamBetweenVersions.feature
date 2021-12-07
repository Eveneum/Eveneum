Feature: Reading stream between versions
	Reading stream including snapshots between certain versions.
	If the stream is shorter than the version provided, the whole stream is returned.

Scenario: Reading stream that doesn't exist
	Given an event store backed by partitioned collection
	When I read stream S from version 10 to version 20
	Then the non-existing stream is returned
	And request charge is reported
	
Scenario: Reading soft-deleted stream
	Given an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I read stream S from version 10 to version 20
	Then the non-existing, soft-deleted stream is returned
	And request charge is reported

Scenario: Reading hard-deleted stream
	Given hard-delete mode
	And an event store backed by partitioned collection
	And a deleted stream S with 20 events
	When I read stream S from version 10 to version 20
	Then the non-existing stream is returned
	And request charge is reported

Scenario: Reading empty stream with no metadata
	Given an event store backed by partitioned collection
	And an existing stream S with 0 events
	When I read stream S from version 0
	Then the stream S in version 0 is returned
	And no snapshot is returned
	And no events are returned
	And request charge is reported
		
Scenario: Reading empty stream with metadata
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 0 events
	When I read stream S from version 0 to version 20
	Then the stream S with metadata in version 0 is returned
	And no snapshot is returned
	And no events are returned
	And request charge is reported
		
Scenario: Reading stream with no metadata and some events
	Given an event store backed by partitioned collection
	And an existing stream S with 20 events
	When I read stream S from version 5 to version 17
	Then the stream S in version 20 is returned
	And no snapshot is returned
	And events from version 5 to 17 are returned
	And request charge is reported
				
Scenario: Reading stream with metadata and some events
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 20 events
	When I read stream S from version 5 to version 17
	Then the stream S with metadata in version 20 is returned
	And no snapshot is returned
	And events from version 5 to 17 are returned
	And request charge is reported

Scenario: Reading stream with metadata, some events and snapshot in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 20 events
	And an existing snapshot for version 7
	When I read stream S from version 5 to version 17
	Then the stream S with metadata in version 20 is returned
	And a snapshot for version 7 is returned
	And events from version 8 to 17 are returned
	And request charge is reported	

Scenario: Reading stream with metadata, some events and ignored snapshot in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 20 events
	And an existing snapshot for version 7
	When I read stream S from version 5 to version 17 ignoring snapshots
	Then the stream S with metadata in version 20 is returned
	And no snapshot is returned
	And events from version 5 to 17 are returned
	And request charge is reported			
				
Scenario: Reading stream from version that is greater than the stream version
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	When I read stream S from version 100 to version 120
	Then the stream S with metadata in version 10 is returned
	And no snapshot is returned
	And no events are returned
	And request charge is reported
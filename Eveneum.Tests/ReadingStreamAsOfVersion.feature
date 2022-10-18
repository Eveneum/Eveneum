Feature: Reading stream as of version
	Reading stream including snapshots up to a certain version.
	If the stream is shorter than the version provided, the whole stream is returned.

Scenario: Reading stream that doesn't exist
	Given an event store backed by partitioned collection
	When I read stream S as of version 10
	Then the non-existing stream is returned
	And request charge is reported
	
Scenario: Reading soft-deleted stream
	Given an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I read stream S as of version 10
	Then the non-existing, soft-deleted stream is returned
	And request charge is reported

Scenario: Reading hard-deleted stream
	Given hard-delete mode
	And an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I read stream S as of version 10
	Then the non-existing stream is returned
	And request charge is reported

Scenario: Reading ttl-deleted stream
	Given ttl-delete mode with 100 seconds as ttl
	And an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I read stream S as of version 10
	Then the non-existing, soft-deleted stream is returned
	And request charge is reported

Scenario: Reading ttl-deleted stream when cosmos disposed the deleted stream
	Given ttl-delete mode with 1 seconds as ttl
	And an event store backed by partitioned collection
	And a deleted stream S with 10 events
	# we need to wait a bit extra, to make sure cosmos cleanup happens.
	When I wait for 2 seconds
	And I read stream S as of version 10
	Then the non-existing stream is returned
	And request charge is reported

Scenario: Reading empty stream with no metadata
	Given an event store backed by partitioned collection
	And an existing stream S with 0 events
	When I read stream S as of version 10
	Then the stream S in version 0 is returned
	And no snapshot is returned
	And no events are returned
	And request charge is reported
		
Scenario: Reading empty stream with metadata
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 0 events
	When I read stream S as of version 10
	Then the stream S with metadata in version 0 is returned
	And no snapshot is returned
	And no events are returned
	And request charge is reported
		
Scenario: Reading stream with no metadata and some events
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I read stream S as of version 10
	Then the stream S in version 10 is returned
	And no snapshot is returned
	And events from version 1 to 10 are returned
	And request charge is reported
				
Scenario: Reading stream with metadata and some events
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	When I read stream S as of version 10
	Then the stream S with metadata in version 10 is returned
	And no snapshot is returned
	And events from version 1 to 10 are returned
	And request charge is reported

Scenario: Reading stream with metadata, some events and snapshot in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot for version 3
	When I read stream S as of version 10
	Then the stream S with metadata in version 10 is returned
	And a snapshot for version 3 is returned
	And events from version 4 to 10 are returned
	And request charge is reported
				
Scenario: Reading stream with no snapshots as of version that is smaller than stream version
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	When I read stream S as of version 5
	Then the stream S with metadata in version 10 is returned
	And events from version 1 to 5 are returned
	And request charge is reported
				
Scenario: Reading stream with a snapshot as of version that is smaller than the snapshot version
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot for version 3
	When I read stream S as of version 5
	Then the stream S with metadata in version 10 is returned
	And a snapshot for version 3 is returned
	And events from version 4 to 5 are returned
	And request charge is reported
				
Scenario: Reading stream with a snapshot as of version that is equal to the snapshot version
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot for version 5
	When I read stream S as of version 5
	Then the stream S with metadata in version 10 is returned
	And a snapshot for version 5 is returned
	And no events are returned
	And request charge is reported
				
Scenario: Reading stream with a snapshot as of version that is greater than the snapshot version
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot for version 7
	When I read stream S as of version 5
	Then the stream S with metadata in version 10 is returned
	And no snapshot is returned
	And events from version 1 to 5 are returned
	And request charge is reported
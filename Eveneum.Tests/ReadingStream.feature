Feature: Reading stream
	Reading stream including latest snapshot

Scenario: Reading stream that doesn't exist
	Given an event store backed by partitioned collection
	When I read stream S
	Then the non-existing stream is returned

Scenario: Reading empty stream with no metadata
	Given an event store backed by partitioned collection
	And an existing stream S with 0 events
	When I read stream S
	Then the stream S in version 0 is returned
	And no snapshot is returned
	And no events are returned
		
Scenario: Reading empty stream with metadata
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 0 events
	When I read stream S
	Then the stream S with metadata in version 0 is returned
	And no snapshot is returned
	And no events are returned
		
Scenario: Reading stream with no metadata and some events
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I read stream S
	Then the stream S in version 10 is returned
	And no snapshot is returned
	And events from version 1 to 10 are returned
				
Scenario: Reading stream with metadata and some events
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	When I read stream S
	Then the stream S with metadata in version 10 is returned
	And no snapshot is returned
	And events from version 1 to 10 are returned

Scenario: Reading stream with no metadata and many events
	Given an event store backed by partitioned collection
	And an existing stream S with 1000 events
	When I read stream S
	Then the stream S in version 1000 is returned
	And no snapshot is returned
	And events from version 1 to 1000 are returned
				
Scenario: Reading stream with metadata and many events
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 1000 events
	When I read stream S
	Then the stream S with metadata in version 1000 is returned
	And no snapshot is returned
	And events from version 1 to 1000 are returned
		
Scenario: Reading stream with no metadata, some events and snapshot in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 3
	When I read stream S
	Then the stream S in version 10 is returned
	And a snapshot for version 3 is returned
	And events from version 4 to 10 are returned
				
Scenario: Reading stream with metadata, some events and snapshot in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot for version 3
	When I read stream S
	Then the stream S with metadata in version 10 is returned
	And a snapshot for version 3 is returned
	And events from version 4 to 10 are returned
				
Scenario: Reading stream with no metadata, some events and snapshot at the end of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 10
	When I read stream S
	Then the stream S in version 10 is returned
	And a snapshot for version 10 is returned
	And no events are returned
				
Scenario: Reading stream with metadata, some events and snapshot and the end of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot for version 10
	When I read stream S
	Then the stream S with metadata in version 10 is returned
	And a snapshot for version 10 is returned
	And no events are returned

Scenario: Reading stream with no metadata, some events and snapshots in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 3
	And an existing snapshot for version 5
	And an existing snapshot for version 7
	When I read stream S
	Then the stream S in version 10 is returned
	And a snapshot for version 7 is returned
	And events from version 8 to 10 are returned
				
Scenario: Reading stream with metadata, some events and snapshots in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot for version 3
	And an existing snapshot for version 5
	And an existing snapshot for version 7
	When I read stream S
	Then the stream S with metadata in version 10 is returned
	And a snapshot for version 7 is returned
	And events from version 8 to 10 are returned
				
Scenario: Reading stream with no metadata, some events and snapshots at the end of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 5
	And an existing snapshot for version 7
	And an existing snapshot for version 10
	When I read stream S
	Then the stream S in version 10 is returned
	And a snapshot for version 10 is returned
	And no events are returned
				
Scenario: Reading stream with metadata, some events and snapshots and the end of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot for version 5
	And an existing snapshot for version 7
	And an existing snapshot for version 10
	When I read stream S
	Then the stream S with metadata in version 10 is returned
	And a snapshot for version 10 is returned
	And no events are returned
		
Scenario: Reading stream with no metadata, some events and snapshot with metadata in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot with metadata for version 3
	When I read stream S
	Then the stream S in version 10 is returned
	And a snapshot with metadata for version 3 is returned
	And events from version 4 to 10 are returned
				
Scenario: Reading stream with metadata, some events and snapshot with metadata in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot with metadata for version 3
	When I read stream S
	Then the stream S with metadata in version 10 is returned
	And a snapshot with metadata for version 3 is returned
	And events from version 4 to 10 are returned
				
Scenario: Reading stream with no metadata, some events and snapshot with metadata at the end of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot with metadata for version 10
	When I read stream S
	Then the stream S in version 10 is returned
	And a snapshot with metadata for version 10 is returned
	And no events are returned
				
Scenario: Reading stream with metadata, some events and snapshot with metadata and the end of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot with metadata for version 10
	When I read stream S
	Then the stream S with metadata in version 10 is returned
	And a snapshot with metadata for version 10 is returned
	And no events are returned

Scenario: Reading stream with no metadata, some events and snapshots with metadata in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 3
	And an existing snapshot for version 5
	And an existing snapshot with metadata for version 7
	When I read stream S
	Then the stream S in version 10 is returned
	And a snapshot with metadata for version 7 is returned
	And events from version 8 to 10 are returned
				
Scenario: Reading stream with metadata, some events and snapshots with metadata in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot for version 3
	And an existing snapshot for version 5
	And an existing snapshot with metadata for version 7
	When I read stream S
	Then the stream S with metadata in version 10 is returned
	And a snapshot with metadata for version 7 is returned
	And events from version 8 to 10 are returned
				
Scenario: Reading stream with no metadata, some events and snapshots with metadata at the end of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 5
	And an existing snapshot for version 7
	And an existing snapshot with metadata for version 10
	When I read stream S
	Then the stream S in version 10 is returned
	And a snapshot with metadata for version 10 is returned
	And no events are returned
				
Scenario: Reading stream with metadata, some events and snapshots with metadata and the end of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot for version 5
	And an existing snapshot for version 7
	And an existing snapshot with metadata for version 10
	When I read stream S
	Then the stream S with metadata in version 10 is returned
	And a snapshot with metadata for version 10 is returned
	And no events are returned
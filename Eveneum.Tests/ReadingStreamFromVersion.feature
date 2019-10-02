Feature: Reading stream from version
	Reading stream events from certain version only.
	If the stream is shorter than the version provided, no events are returned.

Scenario: Reading stream that doesn't exist
	Given an event store backed by partitioned collection
	When I read stream S from version 10
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
	When I read stream S from version 0
	Then the stream S with metadata in version 0 is returned
	And no snapshot is returned
	And no events are returned
	And request charge is reported
		
Scenario: Reading stream with no metadata and some events
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I read stream S from version 5
	Then the stream S in version 10 is returned
	And no snapshot is returned
	And events from version 5 to 10 are returned
	And request charge is reported
				
Scenario: Reading stream with metadata and some events
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	When I read stream S from version 5
	Then the stream S with metadata in version 10 is returned
	And no snapshot is returned
	And events from version 5 to 10 are returned
	And request charge is reported

Scenario: Reading stream with metadata, some events and snapshot in the middle of the stream
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	And an existing snapshot for version 7
	When I read stream S from version 5
	Then the stream S with metadata in version 10 is returned
	And no snapshot is returned
	And events from version 5 to 10 are returned
	And request charge is reported			
				
Scenario: Reading stream from version that is greater than the stream version
	Given an event store backed by partitioned collection
	And an existing stream S with metadata and 10 events
	When I read stream S from version 100
	Then the stream S with metadata in version 10 is returned
	And no snapshot is returned
	And no events are returned
	And request charge is reported
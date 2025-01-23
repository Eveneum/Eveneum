Feature: Creating New Stream
	A new stream is created when streamId doesn't already exist

Scenario: Creating new stream with no metadata and no events
	Given an event store backed by partitioned collection
	When I write a new stream S with 0 events
	Then the header version 0 with no metadata is persisted
	And no events are appended
	And request charge is reported

Scenario: Creating new stream with metadata and no events
	Given an event store backed by partitioned collection
	When I write a new stream S with metadata and 0 events
	Then the header version 0 with metadata is persisted
	And no events are appended
	And request charge is reported
		
Scenario: Creating new stream with no metadata and some events
	Given an event store backed by partitioned collection
	When I write a new stream S with 10 events
	Then the header version 10 with no metadata is persisted
	And new events are appended
	And request charge is reported
				
Scenario: Creating new stream with metadata and some events
	Given an event store backed by partitioned collection
	When I write a new stream S with metadata and 10 events
	Then the header version 10 with metadata is persisted
	And new events are appended
	And request charge is reported
				
Scenario: Creating new stream with no metadata and many events
	Given an event store backed by partitioned collection
	When I write a new stream S with 1000 events
	Then the header version 1000 with no metadata is persisted
	And new events are appended
	And request charge is reported
			
Scenario: Creating new stream with metadata and many events
	Given an event store backed by partitioned collection
	When I write a new stream S with metadata and 9990 events
	Then the header version 9990 with metadata is persisted
	And new events are appended
	And request charge is reported
				
Scenario: Creating new stream with metadata and some events - camel-case contract resolver
	Given Cosmos serializer with camel-case contract resolver
	And an event store backed by partitioned collection
	When I write a new stream S with metadata and 10 events
	Then the header version 10 with metadata is persisted
	And new events are appended
	And request charge is reported
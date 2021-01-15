Feature: Advanced
	Advanced usage scenarios of Event Store

Scenario: Loading all events
	Given an event store backed by partitioned collection
	And a deleted stream A with 10 events
	And an existing stream B with 100 events
	And an existing snapshot for version 10
	And an existing snapshot for version 20
	And an existing snapshot for version 30
	And an existing snapshot for version 40
	And an existing snapshot for version 50
	And an existing stream C with 200 events
	And an existing snapshot for version 200
	And a deleted stream D with 50 events
	And an existing stream I with 0 events
	And an existing stream J with 10 events
	When I load all events
	Then all 370 events are loaded
	And request charge is reported
		
Scenario: Querying events
	Given an event store backed by partitioned collection
	And a deleted stream A with 10 events
	And an existing stream B with 100 events
	And an existing snapshot for version 10
	And an existing snapshot for version 20
	And an existing snapshot for version 30
	And an existing snapshot for version 40
	And an existing snapshot for version 50
	And an existing stream C with 200 events
	And an existing snapshot for version 200
	And a deleted stream D with 50 events
	And an existing stream I with 0 events
	And an existing stream J with 10 events
	When I load events using query text SELECT * from c WHERE c.StreamId = 'B' or c.StreamId = 'D'
	Then all 150 events are loaded
	And request charge is reported
		
Scenario: Querying events excluding deleted ones
	Given an event store backed by partitioned collection
	And a deleted stream A with 10 events
	And an existing stream B with 100 events
	And an existing snapshot for version 10
	And an existing snapshot for version 20
	And an existing snapshot for version 30
	And an existing snapshot for version 40
	And an existing snapshot for version 50
	And an existing stream C with 200 events
	And an existing snapshot for version 200
	And a deleted stream D with 50 events
	And an existing stream I with 0 events
	And an existing stream J with 10 events
	When I load events using query text SELECT * from c WHERE not c.Deleted and (c.StreamId = 'B' or c.StreamId = 'D')
	Then all 100 events are loaded
	And request charge is reported
	
Scenario: Querying events using query definition
	Given an event store backed by partitioned collection
	And a deleted stream A with 10 events
	And an existing stream B with 100 events
	And an existing snapshot for version 10
	And an existing snapshot for version 20
	And an existing snapshot for version 30
	And an existing snapshot for version 40
	And an existing snapshot for version 50
	And an existing stream C with 200 events
	And an existing snapshot for version 200
	And a deleted stream D with 50 events
	And an existing stream I with 0 events
	And an existing stream J with 10 events
	When I load events using query definition SELECT * from c WHERE c.StreamId = 'B' or c.StreamId = 'D'
	Then all 150 events are loaded
	And request charge is reported
		
Scenario: Querying events excluding deleted ones using query definition
	Given an event store backed by partitioned collection
	And a deleted stream A with 10 events
	And an existing stream B with 100 events
	And an existing snapshot for version 10
	And an existing snapshot for version 20
	And an existing snapshot for version 30
	And an existing snapshot for version 40
	And an existing snapshot for version 50
	And an existing stream C with 200 events
	And an existing snapshot for version 200
	And a deleted stream D with 50 events
	And an existing stream I with 0 events
	And an existing stream J with 10 events
	When I load events using query definition SELECT * from c WHERE not c.Deleted and (c.StreamId = 'B' or c.StreamId = 'D')
	Then all 100 events are loaded
	And request charge is reported
	
Scenario: Querying stream headers
	Given an event store backed by partitioned collection
	And an existing stream A with 10 events
	And an existing stream B with 100 events
	And an existing snapshot for version 10
	And an existing snapshot for version 20
	And an existing snapshot for version 30
	And an existing snapshot for version 40
	And an existing snapshot for version 50
	And an existing stream C with 200 events
	And an existing snapshot for version 200
	And an existing stream D with 50 events
	And an existing stream I with 0 events
	And an existing stream J with 10 events
	When I load stream headers using query text SELECT * from c WHERE c.DocumentType = 'Header' and not c.Deleted and c.Version > 75
	Then the stream header for stream B in version 100 is returned
	And the stream header for stream C in version 200 is returned
	And the stream header for stream A in version 10 is not returned
	And the stream header for stream D in version 50 is not returned
	And the stream header for stream I in version 0 is not returned
	And the stream header for stream J in version 10 is not returned
	And request charge is reported
		
Scenario: Querying stream headers excludes deleted streams
	Given an event store backed by partitioned collection
	And an existing stream A with 10 events
	And a deleted stream B with 100 events
	And an existing stream C with 200 events
	And an existing snapshot for version 200
	And an existing stream D with 50 events
	And an existing stream I with 0 events
	And an existing stream J with 10 events
	When I load stream headers using query text SELECT * from c WHERE c.DocumentType = 'Header' and not c.Deleted and c.Version > 75
	Then the stream header for stream B in version 100 is not returned
	And request charge is reported
		
Scenario: Querying stream headers including deleted streams
	Given an event store backed by partitioned collection
	And an existing stream A with 10 events
	And a deleted stream B with 100 events
	And an existing stream C with 200 events
	And an existing snapshot for version 200
	And an existing stream D with 50 events
	And an existing stream I with 0 events
	And an existing stream J with 10 events
	When I load stream headers using query text SELECT * from c WHERE c.DocumentType = 'Header' and c.Version > 75
	Then the stream header for stream B in version 100 is returned
	And the stream header for stream C in version 200 is returned
	And the stream header for stream A in version 10 is not returned
	And the stream header for stream D in version 50 is not returned
	And the stream header for stream I in version 0 is not returned
	And the stream header for stream J in version 10 is not returned
	And request charge is reported
	
Scenario: Querying stream headers using query definition
	Given an event store backed by partitioned collection
	And an existing stream A with 10 events
	And an existing stream B with 100 events
	And an existing snapshot for version 10
	And an existing snapshot for version 20
	And an existing snapshot for version 30
	And an existing snapshot for version 40
	And an existing snapshot for version 50
	And an existing stream C with 200 events
	And an existing snapshot for version 200
	And an existing stream D with 50 events
	And an existing stream I with 0 events
	And an existing stream J with 10 events
	When I load stream headers using query definition SELECT * from c WHERE c.DocumentType = 'Header' and not c.Deleted and c.Version > 75
	Then the stream header for stream B in version 100 is returned
	And the stream header for stream C in version 200 is returned
	And the stream header for stream A in version 10 is not returned
	And the stream header for stream D in version 50 is not returned
	And the stream header for stream I in version 0 is not returned
	And the stream header for stream J in version 10 is not returned
	And request charge is reported
	
Scenario: Querying stream headers using query definition excludes deleted streams
	Given an event store backed by partitioned collection
	And an existing stream A with 10 events
	And a deleted stream B with 100 events
	And an existing stream C with 200 events
	And an existing snapshot for version 200
	And an existing stream D with 50 events
	And an existing stream I with 0 events
	And an existing stream J with 10 events
	When I load stream headers using query definition SELECT * from c WHERE c.DocumentType = 'Header' and not c.Deleted and c.Version > 75
	Then the stream header for stream B in version 100 is not returned
	And request charge is reported
	
Scenario: Querying stream headers including deleted streams using query definition
	Given an event store backed by partitioned collection
	And an existing stream A with 10 events
	And a deleted stream B with 100 events
	And an existing stream C with 200 events
	And an existing snapshot for version 200
	And an existing stream D with 50 events
	And an existing stream I with 0 events
	And an existing stream J with 10 events
	When I load stream headers using query definition SELECT * from c WHERE c.DocumentType = 'Header' and c.Version > 75
	Then the stream header for stream B in version 100 is returned
	And the stream header for stream C in version 200 is returned
	And the stream header for stream A in version 10 is not returned
	And the stream header for stream D in version 50 is not returned
	And the stream header for stream I in version 0 is not returned
	And the stream header for stream J in version 10 is not returned
	And request charge is reported

Scenario: Replace event
	Given an event store backed by partitioned collection
	And an existing stream A with 10 events
	And an existing stream B with 100 events
	When I replace event in version 5 in stream B
	Then the event in version 5 in stream B is replaced
	And request charge is reported

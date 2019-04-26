Feature: Advanced: loading all events
	Advanced scenario of loading all events from Event Store

Scenario Outline: Loading all events
	Given an event store backed by <partitioned> collection
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
	When I load all events
	Then all 370 events are loaded
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

		
Scenario Outline: Querying events
	Given an event store backed by <partitioned> collection
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
	When I load events using query SELECT * from c WHERE c.StreamId = 'B' or c.StreamId = 'D'
	Then all 150 events are loaded
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

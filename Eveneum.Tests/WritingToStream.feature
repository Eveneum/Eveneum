Feature: Writing To Stream
	
Scenario Outline: Creating new stream with no metadata and no events
	Given an event store backed by <partitioned> collection
	When I write a new stream with 0 events
	Then the header version 0 with no metadata is persisted
	And no events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

Scenario Outline: Creating new stream with metadata and no events
	Given an event store backed by <partitioned> collection
	When I write a new stream with metadata and 0 events
	Then the header version 0 with metadata is persisted
	And no events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
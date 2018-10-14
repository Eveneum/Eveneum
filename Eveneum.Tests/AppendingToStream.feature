Feature: Appending to Existing Stream
	
Scenario Outline: Appending to stream with no events
	Given an event store backed by <partitioned> collection
	And an existing stream S with 0 events
	When I append 10 events to stream S in expected version 0
	Then the header version 10 with no metadata is persisted
	And new events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

Scenario Outline: Appending to stream with metadata and no events
	Given an event store backed by <partitioned> collection
	And an existing stream S with metadata and 0 events
	When I append 10 events to stream S in expected version 0
	Then the header version 10 with metadata is persisted
	And new events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

Scenario Outline: Appending to stream with some events
	Given an event store backed by <partitioned> collection
	And an existing stream S with 10 events
	When I append 5 events to stream S in expected version 10
	Then the header version 15 with no metadata is persisted
	And new events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
		
Scenario Outline: Appending to stream with metadata and some events
	Given an event store backed by <partitioned> collection
	And an existing stream S with metadata and 20 events
	When I append 10 events to stream S in expected version 20
	Then the header version 30 with metadata is persisted
	And new events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |



@ExpectException
Scenario Outline: Appending to stream with no events with non-matching expected version fails
	Given an event store backed by <partitioned> collection
	And an existing stream S with 0 events
	When I append 10 events to stream S in expected version 6
	Then the action fails as expected version 6 doesn't match the current version 0 of stream S
	And no events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

@ExpectException
Scenario Outline: Appending to stream with metadata and no events with non-matching expected version fails
	Given an event store backed by <partitioned> collection
	And an existing stream S with metadata and 0 events
	When I append 10 events to stream S in expected version 6
	Then the action fails as expected version 6 doesn't match the current version 0 of stream S
	And no events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

@ExpectException
Scenario Outline: Appending to stream with some events with non-matching expected version fails
	Given an event store backed by <partitioned> collection
	And an existing stream S with 10 events
	When I append 5 events to stream S in expected version 6
	Then the action fails as expected version 6 doesn't match the current version 10 of stream S
	And no events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
		
@ExpectException
Scenario Outline: Appending to stream with metadata and some events with non-matching expected version fails
	Given an event store backed by <partitioned> collection
	And an existing stream S with metadata and 20 events
	When I append 10 events to stream S in expected version 6
	Then the action fails as expected version 6 doesn't match the current version 20 of stream S
	And no events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |
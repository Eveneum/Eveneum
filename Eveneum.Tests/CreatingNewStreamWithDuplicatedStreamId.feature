Feature: Creating new stream with duplicated streamId
	The action fails with StreamAlreadyExists exception when trying to create new stream with duplicated streamId

@ExpectException
Scenario Outline: Creating new stream with no metadata and no events fails if stream id already exists
	Given an event store backed by <partitioned> collection
	And an existing stream S with 5 events
	When I write a new stream S with 0 events
	Then the action fails as stream S already exists
	And no events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

@ExpectException
Scenario Outline: Creating new stream with metadata and no events fails if stream id already exists
	Given an event store backed by <partitioned> collection
	And an existing stream S with 0 events
	When I write a new stream S with metadata and 0 events
	Then the action fails as stream S already exists
	And no events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

@ExpectException
Scenario Outline: Creating new stream with no metadata and some events fails if stream id already exists
	Given an event store backed by <partitioned> collection
	And an existing stream S with 5 events
	When I write a new stream S with 10 events
	Then the action fails as stream S already exists
	And no events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

@ExpectException
Scenario Outline: Creating new stream with metadata and some events fails if stream id already exists
	Given an event store backed by <partitioned> collection
	And an existing stream S with 0 events
	When I write a new stream S with metadata and 100 events
	Then the action fails as stream S already exists
	And no events are appended
	Examples:
		| partitioned     |
		| partitioned     |
		| non-partitioned |

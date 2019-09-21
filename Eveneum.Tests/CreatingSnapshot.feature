Feature: Creating snapshot

Scenario: Creating snapshot
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I create snapshot for stream S in version 5
	Then the snapshot for version 5 is persisted
	And request charge is reported

Scenario: Creating snapshot wth metadata
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I create snapshot with metadata for stream S in version 5
	Then the snapshot for version 5 is persisted
	And request charge is reported

Scenario: Creating earlier snapshot 
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 7
	When I create snapshot for stream S in version 5
	Then the snapshot for version 5 is persisted
	And request charge is reported

Scenario: Creating earlier snapshot with metadata
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 7
	When I create snapshot with metadata for stream S in version 5
	Then the snapshot for version 5 is persisted
	And request charge is reported

Scenario: Creating later snapshot 
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 3
	When I create snapshot for stream S in version 5
	Then the snapshot for version 5 is persisted
	And request charge is reported

Scenario: Creating later snapshot with metadata
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 3
	When I create snapshot with metadata for stream S in version 5
	Then the snapshot for version 5 is persisted
	And request charge is reported

Scenario: Creating snapshot for same version overrides previous one
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 5
	When I create snapshot for stream S in version 5
	Then the snapshot for version 5 is persisted
	And request charge is reported

Scenario: Creating snapshot with metadata for same version overrides previous one
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 5
	When I create snapshot with metadata for stream S in version 5
	Then the snapshot for version 5 is persisted
	And request charge is reported
Feature: Creating snapshot using Snapshot Writer

Scenario: Creating snapshot
	Given a custom Snapshot Writer
	And an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I create snapshot for stream S in version 5
	Then the Snapshot Writer snapshot for version 5 is persisted
	And the custom snapshot for version 5 is persisted 
	And request charge is reported

Scenario: Creating snapshot wth metadata
	Given a custom Snapshot Writer
	And an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I create snapshot with metadata for stream S in version 5
	Then the Snapshot Writer snapshot for version 5 is persisted
	And the custom snapshot for version 5 is persisted 
	And request charge is reported

Scenario: Creating earlier snapshot 
	Given a custom Snapshot Writer
	And an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 7
	When I create snapshot for stream S in version 5
	Then the Snapshot Writer snapshot for version 5 is persisted
	And the custom snapshot for version 5 is persisted 
	And request charge is reported

Scenario: Creating earlier snapshot with metadata
	Given a custom Snapshot Writer
	And an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 7
	When I create snapshot with metadata for stream S in version 5
	Then the Snapshot Writer snapshot for version 5 is persisted
	And the custom snapshot for version 5 is persisted 
	And request charge is reported

Scenario: Creating later snapshot 
	Given a custom Snapshot Writer
	And an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 3
	When I create snapshot for stream S in version 5
	Then the Snapshot Writer snapshot for version 5 is persisted
	And the custom snapshot for version 5 is persisted 
	And request charge is reported

Scenario: Creating later snapshot with metadata
	Given a custom Snapshot Writer
	And an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 3
	When I create snapshot with metadata for stream S in version 5
	Then the Snapshot Writer snapshot for version 5 is persisted
	And the custom snapshot for version 5 is persisted 
	And request charge is reported

Scenario: Creating snapshot for same version overrides previous one
	Given a custom Snapshot Writer
	And an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 5
	When I create snapshot for stream S in version 5
	Then the Snapshot Writer snapshot for version 5 is persisted
	And the custom snapshot for version 5 is persisted 
	And request charge is reported

Scenario: Creating snapshot with metadata for same version overrides previous one
	Given a custom Snapshot Writer
	And an event store backed by partitioned collection
	And an existing stream S with 10 events
	And an existing snapshot for version 5
	When I create snapshot with metadata for stream S in version 5
	Then the Snapshot Writer snapshot for version 5 is persisted
	And the custom snapshot for version 5 is persisted 
	And request charge is reported
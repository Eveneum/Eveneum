Feature: Creating snapshot for non-existent stream
	Creating a snapshot for a stream that doesn't exist fails with StreamNotFound exception

@ExpectException
Scenario: Creating snapshot for non-existent stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I create snapshot for stream X in version 5
	Then the action fails as stream X doesn't exist
	And request charge is reported

@ExpectException
Scenario: Creating snapshot for soft-deleted stream
	Given an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I create snapshot for stream S in version 5
	Then the action fails as stream S doesn't exist
	And the action fails as stream S has been deleted
	And request charge is reported

@ExpectException
Scenario: Creating snapshot for hard-deleted stream
	Given hard-delete mode
	And an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I create snapshot for stream S in version 5
	Then the action fails as stream S doesn't exist
	And request charge is reported

@ExpectException
Scenario: Creating snapshot for ttl-deleted stream
	Given ttl-delete mode with 100 seconds as ttl 
	And an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I create snapshot for stream S in version 5
	Then the action fails as stream S doesn't exist
	And the action fails as stream S has been deleted
	And request charge is reported
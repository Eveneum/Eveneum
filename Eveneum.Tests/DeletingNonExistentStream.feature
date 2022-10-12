Feature: Deleting non-existent stream
	Deleting a stream that doesn't exist fails with StreamNotFound exception

@ExpectException
Scenario: Deleting non-existent stream
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I delete stream X in expected version 10
	Then the action fails as stream X doesn't exist
	And stream S is not soft-deleted
	And request charge is reported

@ExpectException
Scenario: Deleting soft-deleted stream
	Given an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I delete stream S in expected version 10
	Then the action fails as stream S doesn't exist
	And the action fails as stream S has been deleted
	And request charge is reported

@ExpectException
Scenario: Deleting ttl-deleted stream
	Given ttl-delete mode with 100 seconds as ttl
	And an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I delete stream S in expected version 10
	Then the action fails as stream S doesn't exist
	And the action fails as stream S has been deleted
	And request charge is reported

@ExpectException
Scenario: Deleting ttl-deleted stream after cosmos disposed deleted stream
	Given ttl-delete mode with 1 seconds as ttl
	And an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I delete stream S in expected version 10
	And I wait for 2 seconds
	Then the action fails as stream S doesn't exist
	And the action fails as stream S has been deleted
	And request charge is reported

Feature: Deleting already deleted stream
	Deleting a stream that has already been deleted fails with StreamNotFound exception

@ExpectException
Scenario: Deleting already soft-deleted stream
	Given an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I delete stream S in expected version 10
	Then the action fails as stream S doesn't exist
	And the action fails as stream S has been deleted
	And request charge is reported

@ExpectException
Scenario: Deleting already hard-deleted stream
	Given hard-delete mode
	And an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I delete stream S in expected version 10
	Then the action fails as stream S doesn't exist
	And request charge is reported

@ExpectException
Scenario: Deleting already ttl-deleted streams
	Given ttl-delete mode with 100 seconds as ttl
	And an event store backed by partitioned collection
	And a deleted stream S with 10 events
	When I delete stream S in expected version 10
	Then the action fails as stream S doesn't exist
	And the action fails as stream S has been deleted
	And request charge is reported
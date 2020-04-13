Feature: Creating snapshot for non-existing event
	Creating a snapshot for a events version that is higher than stream version fails with OptimisticConcurrency exception

@ExpectException
Scenario: Creating snapshot for non-existent event
	Given an event store backed by partitioned collection
	And an existing stream S with 10 events
	When I create snapshot for stream S in version 20
	Then the action fails as expected version 20 doesn't match the current version 10 of stream S
	And request charge is reported

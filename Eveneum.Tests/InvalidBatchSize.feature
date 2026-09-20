Feature: Invalid batch size
	Creating an event store with a batch size of zero fails with an ArgumentOutOfRangeException

Scenario: Creating an event store with a batch size of 0
	Given a batch size of 0
	When I create an event store
	Then the action fails as the batch size must be greater than zero

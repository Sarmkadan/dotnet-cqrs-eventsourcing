# TestSaga

`TestSaga` is a test fixture class designed to validate the behavior of saga components in a CQRS and event-sourcing context. It exercises saga lifecycle management, event handling, state transitions, and repository persistence operations to ensure correct behavior under various scenarios.

## API

### `public int HandledEvents`

Gets the number of events processed by the saga during its lifetime. This counter is incremented each time the saga handles an event via the `Handle` method.

### `public TestSaga()`

Initializes a new instance of the `TestSaga` class with a null correlation ID. The saga starts in the `NotStarted` state.

### `public TestSaga(string? correlationId)`

Initializes a new instance of the `TestSaga` class with the specified correlation ID. The saga starts in the `NotStarted` state.

**Parameters**
- `correlationId` (string?, optional): The correlation identifier to associate with this saga instance. May be null.

### `public void SetCorrelation(string? id)`

Sets the correlation ID of the saga to the specified value.

**Parameters**
- `id` (string?, optional): The new correlation ID. May be null.

### `public void Handle`

Processes the next pending event in the saga's event queue. This method transitions the saga state according to the event's content and updates internal state accordingly. Throws `InvalidOperationException` if no events are pending or if the saga is already completed.

### `public void Finish`

Forces the saga into the `Completed` state immediately, regardless of pending events. This method does not process any further events and marks the saga as finalized. Throws `InvalidOperationException` if the saga is already completed.

### `public TestSagaHandler`

Gets the saga handler instance associated with this saga. This handler is responsible for routing events to the saga and managing its lifecycle.

### `public async Task<Result> HandleAsync`

Asynchronously processes the next pending event in the saga's queue. This method is the asynchronous counterpart to `Handle` and supports non-blocking event processing. Returns a `Result` indicating success or failure. Throws `InvalidOperationException` if no events are pending or if the saga is already completed.

**Returns**
- `Task<Result>`: A task that represents the asynchronous operation. The result indicates whether the event was processed successfully.

### `public void SagaBase_InitialState_IsNotStarted`

Asserts that a newly created `TestSaga` instance begins in the `NotStarted` state. This method is used in test scenarios to verify initial conditions.

### `public void SagaBase_Activate_TransitionsToActive`

Asserts that invoking activation logic transitions the saga from `NotStarted` to `Active`. This method is used to validate state transition behavior.

### `public void SagaBase_Complete_TransitionsToCompleted`

Asserts that invoking completion logic transitions the saga from `Active` to `Completed`. This method is used to validate state transition behavior.

### `public async Task InMemorySagaRepository_SaveAndRetrieve_RoundTrips`

Asynchronously tests round-trip persistence of a `TestSaga` instance using an in-memory repository. The saga is saved, retrieved, and assertions are made to ensure state and correlation ID are preserved. Throws on any mismatch or persistence failure.

### `public async Task InMemorySagaRepository_FindByCorrelation_ReturnsMatchingSaga`

Asynchronously verifies that the in-memory repository can locate a `TestSaga` by its correlation ID. The method saves a saga, clears internal state, and attempts to retrieve it by correlation. Throws if the saga is not found or if multiple matches are returned.

### `public async Task InMemorySagaRepository_GetAllFilteredByState_ReturnsCorrectSubset`

Asynchronously checks that the repository returns the correct subset of sagas when filtered by state. The method creates multiple sagas in different states, queries by state, and asserts the result count and content. Throws on mismatch.

### `public async Task SagaHandler_ProcessesEvent_PersistsSagaState`

Asynchronously validates that the saga handler processes an incoming event and persists the updated saga state. The method enqueues an event, invokes the handler, and verifies state changes via repository retrieval. Throws on failure to persist or update.

### `public async Task SagaHandler_SecondEvent_AccumulatesOnExistingSaga`

Asynchronously confirms that a second event targeting an existing saga is correctly processed and state is accumulated. The method enqueues two events, processes them sequentially, and asserts final state. Throws if the saga is not found or state is incorrect.

## Usage

### Example 1: Basic Saga Lifecycle

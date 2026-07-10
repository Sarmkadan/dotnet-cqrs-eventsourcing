# InMemoryReadModelStore
The `InMemoryReadModelStore` is a type of read model store that stores data in memory, providing a simple and efficient way to manage read models in an event-sourced system. It allows for upserting, retrieving, querying, and deleting read models, as well as clearing the store and retrieving the count of read models.

## API
* `UpsertAsync`: Upserts a read model in the store. Returns a `Result` indicating the success of the operation.
* `GetAsync<TReadModel>`: Retrieves a read model of type `TReadModel` from the store by its key. Returns a `Result<TReadModel>` containing the read model if found, or an error if not.
* `GetAllAsync`: Retrieves all read models from the store. Returns a `Result<IReadOnlyList<TReadModel>>` containing the list of read models.
* `QueryAsync`: Queries the store for read models matching a specific condition. Returns a `Result<IReadOnlyList<TReadModel>>` containing the list of matching read models.
* `DeleteAsync`: Deletes a read model from the store by its key. Returns a `Result` indicating the success of the operation.
* `GetCountAsync`: Retrieves the count of read models in the store. Returns a `Result<int>` containing the count.
* `ClearAsync`: Clears all read models from the store.
* `GetAllKeys`: Retrieves all keys of read models in the store. Returns an `IReadOnlyList<string>` containing the keys.

## Usage
```csharp
// Example 1: Upserting and retrieving a read model
var readModelStore = new InMemoryReadModelStore();
var readModel = new MyReadModel { Id = "1", Name = "John" };
await readModelStore.UpsertAsync(readModel);
var result = await readModelStore.GetAsync<MyReadModel>("1");
if (result.IsSuccess)
{
    Console.WriteLine(result.Value.Name); // Outputs: John
}

// Example 2: Querying read models
var readModelStore = new InMemoryReadModelStore();
var readModel1 = new MyReadModel { Id = "1", Name = "John" };
var readModel2 = new MyReadModel { Id = "2", Name = "Jane" };
await readModelStore.UpsertAsync(readModel1);
await readModelStore.UpsertAsync(readModel2);
var result = await readModelStore.QueryAsync<MyReadModel>(x => x.Name.StartsWith("J"));
if (result.IsSuccess)
{
    foreach (var readModel in result.Value)
    {
        Console.WriteLine(readModel.Name); // Outputs: John, Jane
    }
}
```

## Notes
The `InMemoryReadModelStore` is not suitable for production use due to its volatile nature, as all data will be lost when the application restarts. It is recommended to use a persistent store in production environments. Additionally, the `InMemoryReadModelStore` is not thread-safe, and concurrent access may lead to unexpected behavior. It is recommended to use synchronization mechanisms or a thread-safe store implementation to ensure data consistency in multi-threaded environments. The `GetAllKeys` property returns a snapshot of the current keys in the store, and may not reflect changes made after the property is accessed.

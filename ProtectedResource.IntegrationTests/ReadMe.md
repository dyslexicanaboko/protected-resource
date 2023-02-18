# Setup
- These tests depend on the BasicDataLayersTests database and the RudimentaryEntity table to exist.
- These can be found here in my [code-snippets](https://github.com/dyslexicanaboko/code-snippets/tree/develop/Visual%20C%23/BasicDataLayers/BasicDataLayers.TestDb) repository in the `BasicDataLayers.TestDb` project folder.
- This is a project I use for having generic test data. Each row contains a variety of common data types.

# Demos
If you want to try this out for yourself to understand how it works follow these steps:
1. Create whatever table you want in the BasicDataLayersTests database or change the connection string to point elsewhere.
1. Make sure to add at least one row to the table so you have something to work with.
1. Create an equivalent class in the `ProtectedResource.Entity` project.
1. Implement the `IResource` interface in the target entity.
1. In the `DemoTests` test fixture, make sure to update the `Demo()` test method to use your table name and entity name.
    1. `TableManager<TResource>` needs to know which entity it is defending, so provide that as `TResource`.
    1. The `TableQuery` needs to match the exact schema of the table. The Entity (TResource) and table name do not have to match.
    1. Create some data requests for your Entity.
    1. Make sure the `ChunkSize` is equal to the number of your total entities to make sure the queue is processed immediately.
    1. Set the `PartitionWatcherMilliseconds` to 120,000 ms to make sure that the internal queue does not kick off if you want to step through the code.
       1.  If your expected results are skewed, it could be that the queue kicked off. Just increase the time to something unreasonably high for the purposes of testing only.
    1. Run the unit test. It will have output on what was recieved by the queue.
1. When finished running the test, go inspect the data manually to make sure it did what you expected.

Running demos like this will skip the Rabbit Queue entirely by design.

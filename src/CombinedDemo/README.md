# Combined Performance Demo

This demo app combines the other three simple demo projects into one
(somewhat contrived) API that demonstrates all of the problems of the others. 
The API finds all products in a certain category that are currently in stock 
and returns MD5 hashes of their product photos.

This demo project has four endpoints:

* **/test/test1** is the slowest of the APIs. It is synchronous, allocates 
unnecessary large `byte[]`s and makes many small database queries.
* **/test/test2** uses asynchronous APIs and does not call `Task.Result`
or `Task.Wait()`.
* **/test/test3** includes the improvements from test2. It also uses
`ArrayPool<T>` instead of allocating and cleaning up many `byte[]`s.
* **/test/test4** improves on test3 by consolidating database access 
to just two queries. It is the fastest of the four APIs (though all
give the same results).

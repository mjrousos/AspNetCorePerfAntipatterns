# ASP.NET Core Perf Demos

### **The projects in this repository are intentionally buggy and do not represent best practices**

This repository contains ASP.NET Core projects that intentionally have
performance-impacting bugs that can be used to demo performance 
diagnostic tools like 
[PerfCollect](https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/linux-performance-tracing.md) and 
[PerfView](https://github.com/Microsoft/perfview).

## Projects

* [Blocking Calls](/src/BlockingCalls) demonstrates the effects of waiting 
synchronously for long-running operations like database access or remote 
service calls. This demo runs quickly when run just once (even the slow path),
but will slow down over time when run under load as the thread pool is 
exchausted and new threads aren't added quickly enough to keep up with 
incoming load. The fast endpoint addresses this issue by making request 
processing async.
* [Inefficient Data Access](/src/ChattyDataAccess) demonstrates the effects of 
interacting with remote services and databases inefficiently ("chatty" 
communication). The slow path in this demo makes repeated calls to the 
database while the fast path makes just two queries. The performance impact is
immediately apparent.
* [Large object allocations](/src/LOHAllocations) demonstrates the effects of 
allocating and cleaning up many large objects. The slow path will run quickly 
when run only once. Under load, however, performance will be inconsistent - 
fast at first, but then periodically becoming much slower as request 
processing is paused for gen 2 garbage collections. The fast path avoids this 
problem by renting shared `byte[]`s from an `ArrayPool` instead of repeatedly
allocating and freeing large arrays.
* [Combined test](/src/CombinedDemo) combines all three other problems in one 
(very buggy) demo application. Instead of the /fast and /slow endpoints 
exposed by the other web APIs, this one has four endpoints (/test1, /test2, 
/test3, and /test4) demonstrating different stages of correcting the various 
performance problems. The first API (/test1) exhibits all three problems 
whereas /test4 has them all corrected.

The [PerfTests](/src/PerfTests) folder contains trivial web tests and load tests 
that can be used to stress the demo apps. `TestAPISlow` and `TestAPIFast` 
target the slow and fast endpoints, respectively, of the first three 
scenarios. `TestAPI` is meant to be used with the combined demo, but it is 
necessary to update `TestAPI.webtest` to point at the particular endpoint 
(/test1, /test2, /test3, or /test4) that you want to exercise.

Since the perf tests only hit a single endpoint each, you can also test with 
simpler performance tools like 
[Apache Bench](https://httpd.apache.org/docs/2.4/programs/ab.html), 
[Siege](https://github.com/JoeDog/siege), 
[Hey](https://github.com/rakyll/hey),
or any number of other load testing tools.

## Requirements, Building, and Running

All projects can be built and launched with the [dotnet CLI](https://docs.microsoft.com/dotnet/core/tools/) 
(`dotnet run`).

### Database Requirement
All of the scenarios except for 
[large object allocations](/src/LOHAllocations) depend on the AdeventureWorks 
sample database. To setup the database, either create an Azure SQL database 
(pre-populating with AdventureWorks data is an option in the Azure portal
while creating the database) or setup the database locally using the 
[AdventureWorks Installation instructions](https://docs.microsoft.com/sql/samples/adventureworks-install-configure?view=sql-server-2017).
# Call Remote Services and Databases Efficiently

The slowest part of many ASP.NET Core applications is calls to data stores or 
other remote services. Because of this, it's critical to minimize and 
optimize such calls. 

Several options are available for improving performance in these areas:

* Data that can be slightly stale (a minute or two out-of-date, perhaps) can 
be [cached](https://docs.microsoft.com/aspnet/core/performance/caching) so 
that they don't need to be queried for every request.
* When retrieving data, try to condense data access into as few queries as 
possible. In some cases, it's even worthwhile to preemptively query for some 
extra rows if you know that the current request is likely to be followed by 
others that will need that data and it can be cached as part of this data 
store access.
* Make sure to only query for necessary data. Don't retrieve rows that aren't 
used. Similarly, make sure that LINQ filters and aggregators (`Where`, 
`Select`, `Sum`, etc.) run before resolving the LINQ query (by iterating over 
it or using `ToList`, for example) so that those operations are completed by 
the database and extra data isn't returned to your service.
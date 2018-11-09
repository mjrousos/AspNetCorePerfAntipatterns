# Minimize Large Object Allocations in Hot Code Paths

Large objects (> 85 kB) are allocated on the large object heap.
Cleaning these up requires a gen-2 garbage collection, which
is slow and can lead to pauses in the app.

To avoid these slowdowns, it's important to not allocate too
many large objects on very hot code paths. This sample demonstrates
a problematic ASP.NET Core app that retrieves a large image 
(from an HTTP request) and converts it to a (large) string resulting
in a couple LOH allocations.


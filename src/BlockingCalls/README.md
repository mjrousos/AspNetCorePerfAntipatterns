# Avoid Blocking Calls

ASP.NET Core applications run highly parallelized. In order for them to run 
efficiently, it's crucial to not block threads. This means:

* Use async APIs for any long-running operations (as opposed to synchronous 
alternatives)
* Don't use `Task.Wait()` or `Task.Result` to wait for tasks to complete
* Don't acquire locks in common code paths

Making one method async will require its callers to be async. In this way, 
making code asynhronous is viral. The changes are worthwhile, though. Even 
a few blocking code paths 
[can cause noticeable performance issues](https://blogs.msdn.microsoft.com/vancem/2018/10/16/diagnosing-net-core-threadpool-starvation-with-perfview-why-my-service-is-not-saturating-all-cores-or-seems-to-stall/). 

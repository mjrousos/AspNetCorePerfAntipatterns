# ASP.NET Core Perf Demos

### **The projects in this repository are intentionally buggy and do not represent best practices**

This repository contains ASP.NET Core projects that intentionally have
performance-impacting bugs that can be used to demo performance 
diagnostic tools like 
[PerfCollect](https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/linux-performance-tracing.md) and 
[PerfView](https://github.com/Microsoft/perfview).

Each folder contains a short description of the performance issue that project
demonstrates. All projects can be built and launched with the dotnet CLI 
(`dotnet run`).

Instructions on supplying to load to the tests to reveal the performance 
issues will be added to this document soon.
# InteropManager
(WIP) A C# library for low level process interoperability like reading / writing process memory, injecting dlls, etc...

---
##### Disclaimer

###### This documentation is Work-In-Progress too, so it may not necessarily be complete or up-to-date.
---
To specify a target process use
```
Target target = Target.Create(...)
```
You can then use
```
target.Attach(...)
```
to open the process for low level memory operations, like reading / writing memory or calculating Cheat Engine pointer paths. These methods can be accessed via
```
target.MemoryManager
```
Alternatively 
```
target.SendKeys(...)
```
can be used to send keystrokes to any process that has a window.
There are also some custom extensions (i.e. `process.GetParentProcess()`).

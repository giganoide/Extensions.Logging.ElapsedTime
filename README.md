# Extensions.Logging.ElapsedTime
Extends Microsoft.Extensions.Logging.ILogger with timed operations

### Installation

The library is published as _Extensions.Logging.ElapsedTime_ on NuGet.

```powershell
Install-Package Extensions.Logging.ElapsedTime
```

### Getting started

Types are in the `SerilogTimings` namespace.

```csharp
using Extensions.Logging.ElapsedTime;
```

#### TimeOperation

The simplest use case is to time an operation, without explicitly recording success/failure:

```csharp
using (_logger.TimeOperation("OperationX"))
{
    // Timed block of code goes here
}
```

At the completion of the `using` block, a message will be written to the log like:

```
info: OperationX completed in 1016.6 ms
```

#### BeginOperation

Operations that can either *succeed or fail*, can be created with `Operation.Begin()`:

```csharp
using var operation = _logger.BeginOperation("OperationX");
// Timed block of code goes here
operation.Complete(true);
```

At the completion of the `using` block, a message will be written to the log like:

```
info: OperationX Ok in 1015.9 ms
```

In case of failure:

```csharp
using var operation = _logger.BeginOperation("OperationX");
// Timed block of code goes here
operation.Complete(false);
```

At the completion of the `using` block, a message will be written to the log like:

```
warn: OperationX Ko in 1007.4 ms
```

#### Abandon

An Operation can be also abondon or cancel:

```csharp
using var operation = _logger.BeginOperation("OperationX");
// Timed block of code goes here
operation.Abandon();
```

At the completion of the `using` block, a message will be written to the log like:

```
warn: OperationX abandoned in 1016.0 ms
```



##### Credits

Inspired by https://github.com/nblumhardt/serilog-timings

# Dependency Injection Refactoring - Event Log Snapshot Storage

## Overview

This refactoring eliminates the static `EventLogSnapshots` property in `McpServerEventLogToolType` and replaces it with proper dependency injection using a singleton service.

## Changes Made

### 1. New File: `EventLogSnapshotStorage.cs`

Created a new service to manage event log snapshot storage:

- **`IEventLogSnapshotStorage`** - Interface defining the storage contract
  - `AddSnapshot()` - Store a new snapshot
  - `TryGetSnapshot()` - Retrieve a snapshot by ID
  - `GetAllSnapshots()` - Get all stored snapshots

- **`EventLogSnapshotEntry`** - Record type for snapshot data
  - `XPathQuery` - The query used to generate the snapshot
  - `JsonContent` - The snapshot JSON content

- **`EventLogSnapshotStorage`** - Default implementation using `ConcurrentDictionary`
  - Thread-safe singleton storage
  - In-memory storage (can be easily replaced with persistent storage)

### 2. Updated: `McpServerEventLogToolType.cs`

**Before:**
```csharp
public static ConcurrentDictionary<string, (string XPathQuery, string JsonContent)> EventLogSnapshots { get; } = new();
```

**After:**
```csharp
private readonly IEventLogSnapshotStorage _snapshotStorage;

public McpServerEventLogToolType(
    ILogger<McpServerEventLogToolType> logger,
    IEventLogSnapshotStorage snapshotStorage)
{
    _logger = logger;
    _snapshotStorage = snapshotStorage;
}
```

- Removed static property
- Added constructor injection
- Updated `CreateEventLogSnapshot()` to use `_snapshotStorage.AddSnapshot()`
- Updated `GetAllEventLogSnapshotResources()` to use `_snapshotStorage.GetAllSnapshots()`

### 3. Updated: `McpServerEventLogResourceType.cs`

**Before:**
```csharp
if (McpServerEventLogToolType.EventLogSnapshots.TryGetValue(id, out var entry))
```

**After:**
```csharp
private readonly IEventLogSnapshotStorage _snapshotStorage;

public McpServerEventLogResourceType(
    ILogger<McpServerEventLogResourceType> logger,
    IEventLogSnapshotStorage snapshotStorage)
{
    _logger = logger;
    _snapshotStorage = snapshotStorage;
}
```

- Added constructor injection
- Updated `GetEventLogSnapshotContent()` to use `_snapshotStorage.TryGetSnapshot()`

### 4. Updated: `SimpleResourceHttpServer.cs`

**Before:**
```csharp
if (McpServerEventLogToolType.EventLogSnapshots.TryGetValue(id, out var entry))
```

**After:**
```csharp
private readonly IEventLogSnapshotStorage _snapshotStorage;

public SimpleResourceHttpServer(int port, IEventLogSnapshotStorage snapshotStorage)
{
    _port = port;
    _snapshotStorage = snapshotStorage;
    // ...
}
```

- Added constructor injection
- Updated `HandleRequest()` to use `_snapshotStorage.TryGetSnapshot()`

### 5. Updated: `Program.cs`

**Before:**
```csharp
var httpServer = new SimpleResourceHttpServer(5555);
httpServer.Start();
```

**After:**
```csharp
// Register event log snapshot storage as singleton
builder.Services.AddSingleton<IEventLogSnapshotStorage, EventLogSnapshotStorage>();

// Register and start HTTP server as singleton
builder.Services.AddSingleton<SimpleResourceHttpServer>(sp =>
{
    var storage = sp.GetRequiredService<IEventLogSnapshotStorage>();
    var httpServer = new SimpleResourceHttpServer(5555, storage);
    httpServer.Start();
    return httpServer;
});

// ...

var app = builder.Build();

// Ensure HTTP server is initialized
_ = app.Services.GetRequiredService<SimpleResourceHttpServer>();
```

- Registered `IEventLogSnapshotStorage` as singleton
- Registered `SimpleResourceHttpServer` as singleton with proper DI
- HTTP server initialization moved into DI container

## Benefits

### 1. **Testability**
- Can easily mock `IEventLogSnapshotStorage` for unit tests
- No more static state that persists between tests

### 2. **Flexibility**
- Easy to swap implementations (e.g., file-based, database, Redis)
- Can add caching, expiration policies, or persistence

### 3. **Separation of Concerns**
- Storage logic is isolated in its own class
- Classes don't depend on static state

### 4. **Thread Safety**
- Storage is properly scoped as singleton
- All classes share the same instance through DI

### 5. **Lifetime Management**
- Storage lifetime is managed by DI container
- Proper disposal through container

## Future Enhancements

The new architecture makes it easy to add:

1. **Persistent Storage**
   ```csharp
   public class FileBasedSnapshotStorage : IEventLogSnapshotStorage
   {
       // Store snapshots in files
   }
   ```

2. **Expiration Policy**
   ```csharp
   public class ExpiringSnapshotStorage : IEventLogSnapshotStorage
   {
       // Automatically expire old snapshots
   }
   ```

3. **Cache Layer**
   ```csharp
   public class CachedSnapshotStorage : IEventLogSnapshotStorage
   {
       // Add memory cache with disk persistence
   }
   ```

4. **Metrics and Monitoring**
   ```csharp
   public class InstrumentedSnapshotStorage : IEventLogSnapshotStorage
   {
       // Add logging, metrics, telemetry
   }
   ```

## Testing

After building (make sure to stop any running instances first):

```powershell
# Stop any running instances
# Then build
dotnet build

# Run the server
dotnet run --project WinDiagMcpServer/WinDiagMcpServer.csproj

# Test with MCP Inspector
mcp-inspector dotnet run --project WinDiagMcpServer/WinDiagMcpServer.csproj
```

All functionality remains the same - the refactoring is purely internal.

## Architecture Diagram

```
┌─────────────────────────────────────────┐
│         DI Container                    │
│  ┌────────────────────────────────┐    │
│  │  IEventLogSnapshotStorage      │    │
│  │  (Singleton)                   │    │
│  └────────────────────────────────┘    │
│              ▲         ▲         ▲      │
│              │         │         │      │
│  ┌───────────┴──┐ ┌───┴────┐ ┌──┴──────┴──┐
│  │ToolType      │ │Resource│ │HTTP Server  │
│  │              │ │Type    │ │             │
│  └──────────────┘ └────────┘ └─────────────┘
└─────────────────────────────────────────┘
```

## Summary

This refactoring successfully:
- ✅ Eliminates static state
- ✅ Implements proper dependency injection
- ✅ Maintains all existing functionality
- ✅ Improves testability and flexibility
- ✅ Follows SOLID principles
- ✅ Makes future enhancements easier

## 1. How diagnostics maps to MCP concepts

Think in terms of one **"System Diagnostics MCP Server"** running on the local machine (C#/.NET 10 is a perfect fit):

### Tools (active operations)

Examples:

* `listProcesses()`
* `getProcessDetails(pid)`
* `killProcess(pid)` *(dangerous → later milestone, gated by confirmation)*
* `getSystemInfo()` (OS, CPU, RAM, etc.)
* `queryEventLog(logName, since)`
* `queryWmi(query)` (e.g. `SELECT * FROM Win32_OperatingSystem`)
* `readRegistry(path)`
* `writeRegistry(path, value)` *(again: only in advanced milestone)*
* `getTopCpuProcesses(n)`
* `getDiskUsage()`

These are **classic MCP tools**: deterministic, side-effectful (some), well-typed.

---

### Resources (read-only diagnostics data)

Treat **snapshots** as resources:

* `/system/eventlogs/recent-application-errors.json`
* `/system/processes/snapshot-{timestamp}.json`
* `/system/wmi/os.json`
* `/system/registry/readonly-config.json`

Workflow:

1. Tool generates data and stores it as a file.
2. Expose those files as **resources**.
3. LLM can then load them in context and reason about them.

This gives you a nice teaching point:

> Tools produce or update resources; LLM *reads* resources.

---

### Prompts (diagnostics recipes)

Define prompts like:

* `analyzeRecentApplicationErrors`

  * params: `logName`, `hoursBack`
  * Uses event log snapshot resource.
* `explainHighCpu`

  * param: `cpuSnapshotResourceUri`
  * Summarizes potential causes.
* `securityAnomalies`

  * Looks for unusual process names, unsigned binaries, etc.

You show students:

* How a server advertises **prompts**
* How prompts encapsulate system-knowledge patterns
* How prompts take parameters and reference resources

---

### Roots (safety boundaries)

Roots are important here because diagnostics is *very powerful*.

Define:

* File-system roots: only allow access to a `DiagnosticsData` folder under your user profile.
* Registry roots: only allow `HKCU\Software\DemoDiagnostics` (for writes) and a few read-only well-known paths for reads.
* WMI: allow only a whitelisted set of classes / namespaces.

Teaching value:

* Demonstrates how a client can **sandbox** what the MCP server may access.
* Excellent illustration of MCP's safety model: *client defines what's visible*.

---

### Elicitation & Sampling (nice fit here)

**Elicitation** examples:

* Tool `killProcess(pid)` is called without `pid` → server asks the client to elicit a process selection from the user.
* Tool `queryEventLog(logName, since)` → if `since` is missing, server uses elicitation to ask:

  > "From when do you want me to collect events? Last hour / 24 hours / custom?"

**Sampling** examples:

* Tool `analyzeSystemHealth()`:

  1. Collects process, event log, and WMI data.
  2. Calls **sampling** to ask the LLM:

     * "Given these resources, produce a human-readable diagnostic report with recommendations."
  3. Returns the report to the user.

Here you get to show:

* The server using the model as a **subroutine**.
* Deterministic diagnostics data + probabilistic explanation layer.

---

## 2. Suggested single-evolving-demo plan (diagnostics edition)

You can reuse the "single demo that evolves" idea, just specialized to diagnostics.

### Milestone 1 – Minimal diagnostics tool (STDIO) ✅ COMPLETE

* MCP server over stdio.
* Tool: `getSystemInfo()`
* Show:

  * JSON-RPC flow,
  * tool list,
  * single call & response.

**Implemented:**
- ✅ `get_system_info` tool
- ✅ STDIO transport
- ✅ MCP Inspector testing

---

### Milestone 2 – Process inspection ✅ COMPLETE

* Add tools:

  * `listProcesses()`
  * `getProcessDetails(pid)`
  * Optional: paging parameters to keep responses small

Teaching points:

* Input/Output schema for tools.
* Handling large result sets (paging, filtering).
* Difference between lightweight summaries and detailed lookups.

**Implemented:**
- ✅ `get_all_processes` tool - Lists all processes with CPU, memory, threads, handles
- ✅ `get_process_info` tool - Detailed process information by PID
- ✅ Comprehensive process metrics (working set, private bytes, virtual memory, etc.)

---

### Milestone 3 – Event log analysis (Resources & Prompts) ✅ COMPLETE

* Tool: `snapshotEventLog(logName, hoursBack)`

  * Writes file into `DiagnosticsData/EventLogs/...json`.
* Expose these files as **resources**.
* Prompt: `analyzeEventLog(resourceUri)`.
* (Optional) Prompt: `explainProcessList(processListResourceUri)` once process snapshots are stored as resources.

Teaching points:

* Resource discovery.
* Separation: **collect** vs **analyze**.
* Read-only resource semantics.
* Prompts that consume resource URIs.

**Implemented:**
- ✅ `create_event_log_snapshot` tool - Creates event log snapshots with XPath filtering
- ✅ Event log resources: `eventlog://snapshot/{id}` with pagination support
- ✅ Resource pagination with query parameters (limit, offset)
- ✅ In-memory snapshot storage
- ✅ **MCP Prompts**:
  - ✅ `AnalyzeRecentApplicationErrors` - Event log error analysis
  - ✅ `ExplainHighCpu` - CPU usage investigation
  - ✅ `DetectSecurityAnomalies` - Security anomaly detection (requires elevation)
  - ✅ `DiagnoseSystemHealth` - Comprehensive health check without elevation ⭐
- ✅ **AI Chat Client (WinDiagMcpChat)**:
  - ✅ Azure OpenAI integration
  - ✅ Automatic prompt discovery
  - ✅ Prompt retrieval tool (`get_prompt`)
  - ✅ Resource reading tool (`read_resource`)
  - ✅ Pagination handling
  - ✅ Conversation history

**Additional Teaching Points Covered:**
- Tool-Resource separation pattern
- MCP Prompts as AI workflow templates
- Parameterized prompts
- Client-side tool for reading resources
- Client-side tool for retrieving prompts
- AI agent using prompts to guide diagnostic workflows
- Handling large data sets with pagination
- Cross-tool correlation (processes + event logs)

---

### Milestone 4 – Move to HTTP + security ✅ COMPLETE

* Convert server to **ASP.NET Core** with **HTTP streaming** transport.
* Add bearer token check (simple demo token).
* Keep same tools and resources.

Teaching points:

* Remote MCP server.
* Basic auth/authorization.
* Path from local tooling to enterprise deployment.

**Implemented:**
- ✅ Converted `WinDiagMcpServer` to ASP.NET Core
- ✅ Implemented HTTP streaming transport
- ✅ Added API Key authentication middleware
- ✅ Updated `WinDiagMcpChat` to use `HttpClientTransport`
- ✅ Configured `WinDiagMcpChat` to launch server in separate window

---

### Milestone 5 – Elicitation for dangerous operations

* Add tool: `killProcess(pid, reason)`
* If `pid` is missing:

  * Use elicitation to present a list of top CPU processes and let user pick.
* Require confirmation (e.g. user types "YES" or selects "Confirm").

Teaching points:

* Human-in-the-loop.
* Elicitation lifecycle.
* Responsible use of powerful tools.

*(You can choose to keep `killProcess` disabled in real demo and just simulate.)*

---

### Milestone 6 – Sampling: system health report

* Tool: `generateSystemHealthReport()`:

  1. Calls other tools internally:

     * `listProcesses()`
     * `snapshotEventLog("Application", 2h)`
     * Maybe `getDiskUsage()`
  2. Calls **sampling** on the client:

     * Prompt: "Generate a diagnostic report from the following resources."
  3. Returns a structured report: summary, potential issues, suggested actions.

Teaching points:

* Multi-tool orchestration inside server.
* Sampling API.
* Server using model as a service.

---

### Milestone 7 – Roots & limitations

* Configure roots such that:

  * Only `DiagnosticsData` folder is visible as resources.
  * Only safe parts of the registry are accessible.
* Show what happens when a tool tries to go outside its allowed root.

Teaching points:

* Sandbox model.
* Why client is in control.
* Security story for enterprise.

---

## 3. Safety & practicality notes

Because this is **system-level access**, I'd strongly recommend:

1. **Read-only first**

   * Start with read-only operations (event logs, process listing, WMI get).
   * Introduce write operations (kill process, registry write) only as optional / simulated.

2. **Explicit confirmation patterns**

   * For any destructive tool (kill, registry write), always:

     * require explicit user confirmation (e.g. "YES, KILL PID 1234"),
     * show how this is enforced in code.

3. **Demo machine**

   * Run these on a clean demo VM or non-critical machine.
   * That's also a good story point about *blast radius*.

These constraints themselves become **excellent teaching content** about how to responsibly use MCP when tools are powerful.


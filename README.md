# 🦴🔌 Caveman.SemanticKernel

**Semantic Kernel plugins for [Caveman](https://www.nuget.org/packages/Caveman)** — the
self-contained, content-aware NLP prompt compressor for LLMs.

This is an **optional add-on package**. The core `Caveman` package has **no Semantic Kernel
dependency**; install this package only if you want to expose Caveman's capabilities as
Semantic Kernel kernel functions.

```bash
dotnet add package Caveman.SemanticKernel
```

Installing it automatically pulls in the `Caveman` core package.

---

## Plugins

| Plugin | Kernel functions |
| :--- | :--- |
| `TokenOptimizerPlugin` | `OptimizePrompt`, `estimate_tokens` |
| `CavemanConversationPlugin` | `summarize_conversation`, `fit_to_budget`, `extract_memory`, `focus_conversation`, `estimate_tokens` |
| `CavemanServicesPlugin` | `generate_commit`, `review_diff`, `check_safety`, `get_stats`, `track_compression`, `investigate_project`, `compress_context`, `reset_stats` |
| `CavemanWikiPlugin` | `generate_project_wiki`, `get_project_summary`, `detect_project_type` |
| **`CavemanRouterPlugin`** *(v1.3.0)* | `route_content`, `detect_content_type`, `compress_json`, `retrieve_ccr`, `analyze_waste`, `batch_route` |
| **`CavemanOutputPlugin`** *(v1.3.0)* | `shape_system_prompt`, `remove_verbosity_steering`, `has_verbosity_steering`, `scan_volatile_tokens`, `has_volatile_tokens`, `optimize_for_cache` |

---

## Quick start

```csharp
using caveman.core.SemanticKernel.Plugin;
using Microsoft.SemanticKernel;

var builder = Kernel.CreateBuilder();
builder.Plugins.AddFromType<CavemanConversationPlugin>();   // agent context tools
builder.Plugins.AddFromType<TokenOptimizerPlugin>();        // NLP prompt compression
builder.Plugins.AddFromType<CavemanRouterPlugin>();         // content-aware routing (v1.3.0)
builder.Plugins.AddFromType<CavemanOutputPlugin>();         // output shaping (v1.3.0)
var kernel = builder.Build();
```

---

## Plugin reference

### TokenOptimizerPlugin
Compresses any prompt text using Caveman's NLP engine (stop-word removal + lemmatization).

| Function | Description |
| :--- | :--- |
| `OptimizePrompt(input, level)` | Compresses text at level 0–3 (None/Light/Semantic/Aggressive). Returns `CompressionResult` with savings metrics. |
| `estimate_tokens(input, model)` | Token count for gpt-4, gpt-3.5, llama-3, gemma-3, claude-3. |

### CavemanConversationPlugin
Gives a model full control over its own context window — no embeddings, no extra LLM calls.

| Function | Description |
| :--- | :--- |
| `summarize_conversation(conversation, parseRoles, keepLastTurns)` | Summarizes only the long natural-language passages; keeps service results and keyword lists verbatim. |
| `fit_to_budget(conversation, maxTokens, model, keepLastTurns)` | Shrinks a conversation to fit a hard token budget. |
| `extract_memory(conversation, maxSentences, maxKeywords)` | Distills salient sentences + key terms for durable agent memory. |
| `focus_conversation(conversation, query, topK)` | Keeps only the blocks most relevant to a query. |
| `estimate_tokens(text, model)` | Token count estimate. |

### CavemanServicesPlugin
Developer services: commit generation, code review, safety checks, stats, project exploration.

| Function | Description |
| :--- | :--- |
| `generate_commit(diffText)` | Conventional commit message from a git diff (`type(scope): subject`). |
| `review_diff(diffText)` | Single-line code review comments (bugs, security, perf, TODOs). |
| `check_safety(message)` | Detects security-critical or destructive command patterns. |
| `get_stats()` | Session token and dollar savings report. |
| `track_compression(originalTokens, compressedTokens)` | Records a compression result into the stats tracker. |
| `investigate_project(projectPath)` | Maps classes, methods and function definitions in a directory. |
| `compress_context(directoryPath)` | Compresses CLAUDE.md, TODO, README.md for AI context windows. |
| `reset_stats()` | Clears session statistics. |

### CavemanWikiPlugin
Generates AI-optimized project documentation on demand.

| Function | Description |
| :--- | :--- |
| `generate_project_wiki(projectPath, maxFileSizeKB, compressionLevel, includeContents)` | Full markdown wiki with compressed file contents. |
| `get_project_summary(projectPath)` | Lightweight metadata + file tree, no contents. |
| `detect_project_type(projectPath)` | Detects C#, Python, Node.js, Java, Rust, etc. Returns JSON. |

### CavemanRouterPlugin *(new in v1.3.0)*
Content-aware compression pipeline: auto-detects content type and applies the best algorithm.

| Function | Description |
| :--- | :--- |
| `route_content(content, query, profile)` | Routes any content through the pipeline. Returns detected type, strategy, token savings and compressed text. Profiles: `balanced` (default), `light`, `agent`, `aggressive`. |
| `detect_content_type(content)` | Classifies content as JsonArray, GitDiff, LogOrStacktrace, Html, Code, Tabular, PlainText, etc. — no compression. |
| `compress_json(jsonArray, query, maxItems)` | SmartCrusher: lossless markdown-table or CSV compaction, or BM25 row-drop with a CCR marker. |
| `retrieve_ccr(ccrHash)` | Retrieves original rows dropped by `compress_json` (5-min TTL). |
| `analyze_waste(content)` | Reports estimated wasted tokens by category: HTML noise, base64 blobs, whitespace, JSON bloat. Non-destructive. |
| `batch_route(contents, delimiter, profile)` | Processes multiple `---`-delimited sections and reports total savings. |

```csharp
// Route a tool result to the best compressor
var result = await kernel.InvokeAsync("CavemanRouterPlugin", "route_content", new KernelArguments
{
    ["content"] = buildLogOutput,
    ["query"]   = "error exception",
    ["profile"] = "agent"
});
```

### CavemanOutputPlugin *(new in v1.3.0)*
Reduces LLM **output** tokens by injecting verbosity-steering into system prompts, and protects
the provider's KV-cache prefix from volatile tokens.

| Function | Description |
| :--- | :--- |
| `shape_system_prompt(systemPrompt, level)` | Appends verbosity instructions at level 0–4. Byte-stable per level; idempotent. |
| `remove_verbosity_steering(systemPrompt)` | Removes any steering previously injected. |
| `has_verbosity_steering(systemPrompt)` | Returns `"true"` / `"false"`. |
| `scan_volatile_tokens(systemPrompt)` | Finds UUIDs, ISO-8601 timestamps, JWTs, hex hashes that bust the KV-cache. |
| `has_volatile_tokens(systemPrompt)` | Quick boolean check. |
| `optimize_for_cache(systemPrompt, verbosityLevel)` | One call: adds verbosity steering + reports volatile tokens. |

**Verbosity levels:**

| Level | Effect |
| :--- | :--- |
| 0 — Off | No injection |
| 1 — SkipCeremony | No "Sure!", "Of course!", "Let me…" |
| 2 — NoRestatement *(default)* | + Never echo back code/files/diffs from context |
| 3 — ConclusionsOnly | + Conclusions only, skip rationale |
| 4 — MinimumTokens | Maximum savings — fragments OK |

```csharp
// Prepare a system prompt for maximum output efficiency
var shaped = await kernel.InvokeAsync("CavemanOutputPlugin", "optimize_for_cache", new KernelArguments
{
    ["systemPrompt"]    = mySystemPrompt,
    ["verbosityLevel"]  = 2
});
```

---

## License & attribution

Released under the **Caveman License** — the MIT License **plus one mandatory condition**:

> Any use of this library must clearly and visibly disclose that it uses
> "Caveman" by Passaro Francesco Paolo (Digitalsolutions.it).

A note like the following, in documentation, an About screen or the repository, satisfies the requirement:

```
Powered by Caveman - (c) Passaro Francesco Paolo, Digitalsolutions.it
```

© 2026 Passaro Francesco Paolo — Digitalsolutions.it

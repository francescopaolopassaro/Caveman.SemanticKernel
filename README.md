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

## Technology Partnership

<img src="https://www.digitalsolutions.it/img/partners/novaroutelogo.png" alt="NovaRouteAI" height="180" style="max-width: 100%; height: auto; min-height: 180px; max-height: 190px;">

**[NovaRouteAI](https://novarouteai.com/?ref=synthelion)** — Build with Chinese AI models through one simple API.

NovaRouteAI helps developers and AI SaaS teams test, compare, and run models like DeepSeek, Qwen, Doubao, Kimi, and GLM without managing multiple provider accounts. Start with test credits and optimize your cost per successful task.

[Click here to know NovaRouteAI](https://novarouteai.com/?ref=synthelion)

---

## Plugins

| Plugin | Kernel functions |
| :--- | :--- |
| `TokenOptimizerPlugin` | `OptimizePrompt` (levels 0–5), `estimate_tokens` |
| `CavemanConversationPlugin` | `summarize_conversation`, `fit_to_budget`, `extract_memory`, `focus_conversation`, `estimate_tokens`, `share_context`, `get_shared_context`, `shared_context_stats`, `dedup_messages`, `clean_duplicate_messages`, `check_near_duplicate` |
| `CavemanServicesPlugin` | `generate_commit`, `review_diff`, `check_safety`, `get_stats`, `track_compression`, `investigate_project`, `compress_context`, `reset_stats`, `skeletonize_code` |
| `CavemanWikiPlugin` | `generate_project_wiki`, `get_project_summary`, `detect_project_type` |
| **`CavemanRouterPlugin`** *(v1.3.0)* | `route_content`, `detect_content_type`, `compress_json`, `retrieve_ccr`, `analyze_waste`, `batch_route` |
| **`CavemanOutputPlugin`** *(v1.3.0)* | `shape_system_prompt`, `remove_verbosity_steering`, `has_verbosity_steering`, `scan_volatile_tokens`, `has_volatile_tokens`, `optimize_for_cache` |
| **`CavemanRetrievalPlugin`** *(v1.4.3)* | `retrieve`, `retrieve_with_feedback` |
| **`CavemanSummarizationPlugin`** *(v1.4.3)* | `summarize_text`, `summarize_text_topic_aware` |

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
builder.Plugins.AddFromType<CavemanRetrievalPlugin>();      // BM25+ retrieval (v1.4.3)
builder.Plugins.AddFromType<CavemanSummarizationPlugin>();  // plain-text summarization (v1.4.3)
var kernel = builder.Build();
```

---

## Plugin reference

### TokenOptimizerPlugin
Compresses any prompt text using Caveman's NLP engine (stop-word removal + lemmatization).

| Function | Description |
| :--- | :--- |
| `OptimizePrompt(input, level)` | Compresses text at level 0–5 (None/Light/Semantic/Aggressive/Statistical/Syntactic). Returns `CompressionResult` with savings metrics. |
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
| `share_context(key, content, agentName)` | Compresses and stores content under a key for inter-agent sharing (TTL 30 min). |
| `get_shared_context(key, full)` | Retrieves the compressed (default) or original content by key. |
| `shared_context_stats()` | Aggregate token savings across all active shared-context entries. |
| `dedup_messages(messages, delimiter)` | Scans a delimiter-separated message sequence for re-read duplicates (exact hash match); reports wasted tokens. |
| `clean_duplicate_messages(messages, delimiter)` | Replaces duplicates with `[duplicate of message #N]` placeholders. |
| `check_near_duplicate(a, b, maxDistance, shingleSize)` | SimHash-based near-duplicate check for two texts (catches templated text, not just exact matches). |

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
| `skeletonize_code(code)` | Strips comments and collapses function/method bodies to signatures only (real brace-depth/indentation parsing, not regex). |

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

### CavemanRetrievalPlugin *(new in v1.4.3)*
BM25+ ranking over arbitrary text chunks — pure term statistics, no embeddings, no model, no network call.

| Function | Description |
| :--- | :--- |
| `retrieve(documents, query, delimiter, topK)` | Ranks `---`-delimited text chunks against a query with plain BM25+. |
| `retrieve_with_feedback(documents, query, delimiter, topK, iso3)` | Same, plus RM3 pseudo-relevance feedback: expands the query with vocabulary from the top initial results before a final re-rank — surfaces relevant chunks that don't literally contain the query's words. |

```csharp
// Rank log lines by relevance to an error investigation
var hits = await kernel.InvokeAsync("CavemanRetrievalPlugin", "retrieve", new KernelArguments
{
    ["documents"] = string.Join("\n---\n", logLines),
    ["query"]     = "connection timeout database",
    ["topK"]      = 5
});
```

### CavemanSummarizationPlugin *(new in v1.4.3)*
Extractive summarization for plain text (use `CavemanConversationPlugin.summarize_conversation` instead for role-tagged transcripts).

| Function | Description |
| :--- | :--- |
| `summarize_text(text, sentences, ratio, algo, iso3)` | TF-IDF (`tfidf`) or TextRank (`textrank`, default) extractive summary. |
| `summarize_text_topic_aware(text, sentences, iso3)` | Segments the text into topics first (TextTiling-style), then allocates the sentence budget proportionally so one dense topic can't starve the rest. Best for long, multi-topic documents. |

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

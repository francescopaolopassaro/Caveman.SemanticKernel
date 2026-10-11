# Changelog

All notable changes to **Caveman.SemanticKernel** are documented in this file.

## [1.4.5] - 2026-10-11

Aligned with **Caveman core 1.4.5**. No API changes: every `compress` / `OptimizePrompt` call now keeps structured values (IPs, versions, dates, URLs) verbatim, never corrupts JSON/code/SQL/YAML (routed to the structure-aware compressors or returned unchanged) and no longer inflates Chinese/Japanese output.

---

## [1.4.4] - 2026-08-13

Aligned with **Caveman core 1.4.4** (referenced as a NuGet dependency). Fully additive release; no breaking changes.

### Added

- `CavemanServicesPlugin.compress_sql` — whitespace-safe SQL compression that preserves string literals, quoted
  identifiers, nested parentheses and comments. Optional `stripComments` (removes `--` and `/* */`) and `foldValues`
  (replaces `VALUES` tuples with a content-derived CCR-hash reference, retrievable within a short TTL via
  `retrieve_ccr`), with `tuples_dropped` reporting how many tuples were folded.
- `CavemanRouterPlugin.detect_content_type` now recognizes `Sql` content (detected before `Code`/`Tabular`).

---

## [1.4.3] - 2026-07-19

Catches this package up to three core versions of drift (1.3.1 → 1.4.x). Now references **Caveman core 1.4.3**
as a NuGet package dependency instead of a project reference (1.4.3 is a corrective re-publish of 1.4.2, which
was packed from a stale snapshot and was missing the features below).

### Added

- `TokenOptimizerPlugin.OptimizePrompt` now accepts levels 0–5 (previously capped at 3): adds `Statistical`
  (TF-IDF word scoring) and `Syntactic` (rule-based grammatical-glue pruning).
- **`CavemanRetrievalPlugin`** (new plugin) — wraps `CavemanRetriever`: `retrieve` (BM25+ ranking of text
  chunks against a query) and `retrieve_with_feedback` (adds RM3 pseudo-relevance feedback query expansion).
- **`CavemanSummarizationPlugin`** (new plugin) — plain-text extractive summarization, previously only
  available for conversation transcripts: `summarize_text` (TF-IDF/TextRank) and `summarize_text_topic_aware`
  (TextTiling-style topic segmentation before sentence-budget allocation).
- `CavemanServicesPlugin.skeletonize_code` — strips comments and collapses function/method bodies to
  signatures only, using real brace-depth (C-family) or indentation (Python) parsing.
- `CavemanConversationPlugin.check_near_duplicate` — SimHash-based near-duplicate detection for two texts,
  complementing the existing exact-hash `dedup_messages`/`clean_duplicate_messages`.

---

## [1.3.1] - 2026-06-24

Aligned with **Caveman core 1.3.1** (corrects the 1.3.0 NuGet package which was missing the new
content-aware pipeline services). Exposes `CavemanSharedContext` and `CavemanMessageDeduplicator`
as Semantic Kernel functions — the last two additions from the 1.3.0 feature set that were not yet
wrapped. All existing plugins are unchanged; fully additive release.

### Added

#### `CavemanConversationPlugin` — five new functions

| Function | What it does |
| :--- | :--- |
| `share_context(key, content, agentName?)` | Compresses content with Caveman NLP and stores it by key in the inter-agent shared context store (TTL 30 min). Returns token savings metadata. |
| `get_shared_context(key, full?)` | Retrieves the compressed copy (default) or the original content by key. Returns an error if the key is unknown or expired. |
| `shared_context_stats()` | Reports aggregate token savings across all active entries in the shared store. |
| `dedup_messages(messages, delimiter?)` | Scans a delimiter-separated message sequence for re-read duplicates and reports wasted tokens. |
| `clean_duplicate_messages(messages, delimiter?)` | Replaces duplicate messages with `[duplicate of message #N]` placeholders and returns the cleaned sequence. |

---

## [1.3.0] - 2026-06-23

Aligned with **Caveman core 1.3.0**. Two new plugins expose the content-aware
compression pipeline and the output-shaping system as Semantic Kernel kernel
functions. All existing plugins are unchanged; fully additive release.

### Added

#### `CavemanRouterPlugin` (new)

Wraps `CavemanContentRouter`, `CavemanContentDetector`, `CavemanJsonCrusher`
and `CavemanWasteAnalyzer`.

| Function | What it does |
| :--- | :--- |
| `route_content(content, query?, profile?)` | Auto-detects content type (JSON array, log, diff, HTML, code, table, prose) and applies the best compressor. Returns detected type, strategy, token savings and compressed text. |
| `detect_content_type(content)` | Classifies content without compressing it. |
| `compress_json(jsonArray, query?, maxItems?)` | SmartCrusher: lossless markdown-table / CSV, or BM25 row-drop with CCR marker. |
| `retrieve_ccr(hash)` | Retrieves rows dropped by `compress_json` from the CCR store (5-min TTL). |
| `analyze_waste(content)` | Estimates wasted tokens by category (HTML, base64, whitespace, JSON). Non-destructive. |
| `batch_route(contents, delimiter?, profile?)` | Processes multiple delimited sections and reports total savings. |

Profiles: `balanced` (default), `light`, `agent`, `aggressive`.

#### `CavemanOutputPlugin` (new)

Wraps `CavemanOutputShaper` and `CavemanCacheAligner`.

| Function | What it does |
| :--- | :--- |
| `shape_system_prompt(prompt, level?)` | Injects verbosity-steering at level 0–4 to reduce LLM output tokens. Byte-stable per level; idempotent. |
| `remove_verbosity_steering(prompt)` | Removes previously injected steering. |
| `has_verbosity_steering(prompt)` | Returns `"true"` / `"false"`. |
| `scan_volatile_tokens(prompt)` | Finds UUIDs, ISO-8601 timestamps, JWTs and hex hashes that invalidate the LLM provider's KV-cache prefix. |
| `has_volatile_tokens(prompt)` | Quick boolean check. |
| `optimize_for_cache(prompt, level?)` | One call: adds verbosity steering + reports volatile tokens. |

Verbosity levels: 0=Off, 1=SkipCeremony, 2=NoRestatement (default), 3=ConclusionsOnly, 4=MinimumTokens.

### Changed

- Package version bumped to **1.3.0** to track Caveman core 1.3.0.
- `README.md` updated with full function reference tables for all six plugins.

---

## [1.0.0] - 2026-06-13

Initial release as a standalone package. Split out of the Caveman core package
so the core no longer forces a Microsoft.SemanticKernel dependency.

### Added

- `TokenOptimizerPlugin` — `OptimizePrompt` (levels 0–3), `estimate_tokens`.
- `CavemanConversationPlugin` — `summarize_conversation`, `fit_to_budget`,
  `extract_memory`, `focus_conversation`, `estimate_tokens`.
- `CavemanServicesPlugin` — `generate_commit`, `review_diff`, `check_safety`,
  `get_stats`, `track_compression`, `investigate_project`, `compress_context`,
  `reset_stats`.
- `CavemanWikiPlugin` — `generate_project_wiki`, `get_project_summary`,
  `detect_project_type`.

[1.3.1]: https://github.com/francescopaolopassaro/caveman-semantickernel/releases/tag/v1.3.1
[1.3.0]: https://github.com/francescopaolopassaro/caveman-semantickernel/releases/tag/v1.3.0
[1.0.0]: https://github.com/francescopaolopassaro/caveman-semantickernel/releases/tag/v1.0.0

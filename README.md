# 🦴🔌 Caveman.SemanticKernel

**Semantic Kernel plugins for [Caveman](https://www.nuget.org/packages/Caveman)** — the
self-contained NLP prompt compressor for LLMs.

This is an **optional add-on package**. The core `Caveman` package has **no Semantic Kernel
dependency**; install this package only if you want to expose Caveman's capabilities as
Semantic Kernel kernel functions.

```bash
dotnet add package Caveman.SemanticKernel
```

Installing it automatically pulls in the `Caveman` core package.

## Plugins

| Plugin | Kernel functions |
| :--- | :--- |
| `TokenOptimizerPlugin` | `OptimizePrompt`, `estimate_tokens` |
| `CavemanConversationPlugin` | `summarize_conversation`, `fit_to_budget`, `extract_memory`, `focus_conversation`, `estimate_tokens` |
| `CavemanServicesPlugin` | `generate_commit`, `review_diff`, `check_safety`, `get_stats`, `track_compression`, `investigate_project`, `compress_context`, `reset_stats` |
| `CavemanWikiPlugin` | `generate_project_wiki`, `get_project_summary`, `detect_project_type` |

## Usage

```csharp
using caveman.core.SemanticKernel.Plugin;
using Microsoft.SemanticKernel;

var builder = Kernel.CreateBuilder();
builder.Plugins.AddFromType<CavemanConversationPlugin>();   // agent context tools
builder.Plugins.AddFromType<TokenOptimizerPlugin>();        // prompt compression
var kernel = builder.Build();
```

The `CavemanConversationPlugin` lets a model manage its own context window: summarize a whole
transcript, fit it to a token budget, distill a durable memory, focus on a query, or estimate
token usage — all locally, with no embeddings and no extra LLM calls.

## License & attribution

Released under the **Caveman License** — the MIT License **plus one mandatory condition**:

> Any use of this library must clearly and visibly disclose that it uses
> "Caveman" by Passaro Francesco Paolo (Digitalsolutions.it).

See [`LICENSE`](LICENSE) for the full terms.

© 2026 Passaro Francesco Paolo — Digitalsolutions.it

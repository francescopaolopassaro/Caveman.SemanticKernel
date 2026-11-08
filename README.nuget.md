# Caveman.SemanticKernel

Semantic Kernel plugins for Caveman, the self-contained NLP prompt compressor for LLMs. This is an optional add-on:
the core Caveman package has no Semantic Kernel dependency. Installing this package pulls in Caveman.

Version 1.5.0 targets .NET 10 and references Caveman 1.5.0.

## Install

    dotnet add package Caveman.SemanticKernel

## Register the plugins

    var builder = Kernel.CreateBuilder();
    builder.Plugins.AddFromType<CavemanConversationPlugin>();
    builder.Plugins.AddFromType<TokenOptimizerPlugin>();
    builder.Plugins.AddFromType<CavemanSemanticCachePlugin>();
    var kernel = builder.Build();

## Plugins

| Plugin | Kernel functions |
| :--- | :--- |
| TokenOptimizerPlugin | OptimizePrompt (levels 0 to 5), estimate_tokens, idf_languages |
| CavemanConversationPlugin | compact_messages, summarize_conversation, fit_to_budget, extract_memory, focus_conversation, estimate_tokens, share_context, get_shared_context, shared_context_stats, dedup_messages, clean_duplicate_messages, check_near_duplicate |
| CavemanSemanticCachePlugin | semantic_cache_get, semantic_cache_set, semantic_cache_stats, semantic_cache_clear |
| CavemanServicesPlugin | generate_commit, review_diff, check_safety, get_stats, track_compression, investigate_project, compress_context, reset_stats, skeletonize_code, compress_sql |
| CavemanRouterPlugin | route_content, detect_content_type, compress_json, retrieve_ccr, analyze_waste, batch_route |
| CavemanOutputPlugin | shape_system_prompt, remove_verbosity_steering, has_verbosity_steering, scan_volatile_tokens, has_volatile_tokens, optimize_for_cache |
| CavemanRetrievalPlugin | retrieve, retrieve_with_feedback |
| CavemanSummarizationPlugin | summarize_text, summarize_text_topic_aware |
| CavemanWikiPlugin | generate_project_wiki, get_project_summary, detect_project_type |

## New in 1.5.0

### Semantic cache

A paraphrase of an earlier query is answered from the cache instead of a new model call. It is algorithmic (no
embeddings, no model, no network) and works in every language Caveman supports.

| Function | Result |
| :--- | :--- |
| semantic_cache_get | JSON with hit, response, similarity, matched_query and language; hit is false on a miss |
| semantic_cache_set | Stores an answer under a query |
| semantic_cache_stats | Entries, hits, misses, hit rate, characters served from the cache |
| semantic_cache_clear | Empties the cache |

Numbers, file names, paths, identifiers and quoted strings must match exactly for a hit, so a request about
Program.cs never matches one about Startup.cs. The default cache is shared by every plugin instance; pass your own
CavemanSemanticCache to the constructor to tune threshold, size or time to live.

### compact_messages

Compacts a messages array (OpenAI-compatible, Anthropic, Amazon Bedrock Converse, Gemini or Cohere JSON) and returns
messages ready to send again.

| Parameter | Meaning |
| :--- | :--- |
| messagesJson | The messages array as JSON |
| maxTokens | Hard token budget, 0 for none |
| keepLastTurns | Recent turns kept verbatim, default 4 |
| format | openai (same roles as the input) or converse (Amazon Bedrock Converse) |
| model | Any model name for token counting |

| Guarantee | Detail |
| :--- | :--- |
| Tool calls kept | Tool calls and tool results are never removed or altered: same ids, same blocks, same payload |
| Structured content kept | JSON, XML, HTML, Markdown, SQL and code are compressed, not stripped |
| Converse output | System prompt in its own field, strict user and assistant alternation, user first turn, no empty turn, toolUse and toolResult blocks |

### Models

Token counting accepts any version name of these families: gpt, claude, gemini, gemma, llama, qwen, deepseek, grok,
glm (Z.ai), spark, mistral, nova, kimi, ernie, command.

## License

MIT with mandatory attribution to Passaro Francesco Paolo, Digitalsolutions.it. See the LICENSE file in the package.

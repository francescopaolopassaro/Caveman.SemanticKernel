// -----------------------------------------------------------------------------
// <copyright file="CavemanSemanticCachePlugin.cs" company="Digitalsolutions.it">
//   Caveman.SemanticKernel — Semantic Kernel plugins for Caveman.
//   Copyright (c) 2026 Passaro Francesco Paolo — Digitalsolutions.it.
//   Licensed under the Caveman License (MIT + mandatory attribution).
// </copyright>
// <summary>SK plugin wrapping CavemanSemanticCache: answers paraphrased queries from a previous answer.</summary>
// -----------------------------------------------------------------------------
using System.ComponentModel;
using System.Text.Json;
using caveman.core.services;
using Microsoft.SemanticKernel;

namespace caveman.core.SemanticKernel.Plugin;

/// <summary>
/// Semantic cache for agents: a paraphrase of an earlier query is answered from the cache instead of paying for a
/// new model call. Algorithmic (no embeddings, no extra model), multilingual (every language Caveman supports).
/// Numbers, file names, identifiers and quoted strings must match exactly for a hit.
/// </summary>
public sealed class CavemanSemanticCachePlugin
{
    // Plugins are usually registered with AddFromType (a new instance per kernel): the default cache is shared so
    // every instance sees the same entries. Pass your own CavemanSemanticCache to isolate or tune it.
    private static readonly CavemanSemanticCache SharedDefault = new();

    private readonly CavemanSemanticCache _cache;

    public CavemanSemanticCachePlugin(CavemanSemanticCache? cache = null) => _cache = cache ?? SharedDefault;

    [KernelFunction("semantic_cache_get")]
    [Description("Looks a query up in the semantic cache. Returns JSON {hit, response, similarity, matched_query, language}; hit=false on a miss.")]
    public string Get([Description("The query to look up")] string query)
    {
        if (_cache.TryGet(query, out var hit))
            return JsonSerializer.Serialize(new { hit = true, response = hit.Response, similarity = Math.Round(hit.Similarity, 3), matched_query = hit.MatchedQuery, language = hit.Language });
        return JsonSerializer.Serialize(new { hit = false });
    }

    [KernelFunction("semantic_cache_set")]
    [Description("Stores an answer under a query so later paraphrases of the query can be answered from the cache.")]
    public string Set(
        [Description("The query the answer belongs to")] string query,
        [Description("The answer to cache")] string response)
    {
        _cache.Set(query, response);
        return JsonSerializer.Serialize(new { stored = true, entries = _cache.Count });
    }

    [KernelFunction("semantic_cache_stats")]
    [Description("Returns entries, hits, misses, hit rate and the characters of answers served from the cache.")]
    public string Stats()
    {
        var s = _cache.Stats;
        return JsonSerializer.Serialize(new { entries = _cache.Count, hits = s.Hits, misses = s.Misses, hit_rate = Math.Round(s.HitRate, 3), chars_saved = s.CharsSaved });
    }

    [KernelFunction("semantic_cache_clear")]
    [Description("Empties the semantic cache.")]
    public string Clear()
    {
        _cache.Clear();
        return JsonSerializer.Serialize(new { cleared = true });
    }
}

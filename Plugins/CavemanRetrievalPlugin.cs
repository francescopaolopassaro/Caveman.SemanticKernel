// -----------------------------------------------------------------------------
// <copyright file="CavemanRetrievalPlugin.cs" company="Digitalsolutions.it">
//   Caveman.SemanticKernel — Semantic Kernel plugins for Caveman.
//   Copyright (c) 2026 Passaro Francesco Paolo — Digitalsolutions.it.
//   Licensed under the Caveman License (MIT + mandatory attribution).
// </copyright>
// <summary>SK plugin wrapping CavemanRetriever (1.4.1): BM25+ ranking over arbitrary text chunks, with optional RM3 pseudo-relevance feedback.</summary>
// -----------------------------------------------------------------------------
using System.ComponentModel;
using System.Text;
using caveman.core.services;
using Microsoft.SemanticKernel;

namespace caveman.core.SemanticKernel.Plugin;

/// <summary>
/// Semantic Kernel plugin exposing <see cref="CavemanRetriever"/>: ranks arbitrary text chunks
/// (sentences, conversation turns, log lines, search results, …) against a query using BM25+.
/// Pure term-frequency statistics — no embeddings, no model, no network call.
/// </summary>
public sealed class CavemanRetrievalPlugin
{
    private readonly CavemanRetriever _retriever;

    public CavemanRetrievalPlugin(CavemanRetriever? retriever = null)
    {
        _retriever = retriever ?? new CavemanRetriever(new FunctionWordProvider());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // retrieve
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("retrieve")]
    [Description("Ranks a list of text chunks (sentences, log lines, search results, conversation turns, …) against a query using BM25+. " +
                 "Chunks are separated by a delimiter. Returns the top-K matches with scores.")]
    public string Retrieve(
        [Description("Delimiter-separated list of text chunks to rank")] string documents,
        [Description("The query to rank chunks against")] string query,
        [Description("Delimiter between chunks (default: ---)")] string delimiter = "---",
        [Description("Maximum number of results to return (default: 5)")] int topK = 5)
    {
        if (string.IsNullOrWhiteSpace(documents) || string.IsNullOrWhiteSpace(query))
            return "[empty input]";

        var docs = documents.Split(new[] { delimiter }, StringSplitOptions.None)
                             .Select(d => d.Trim())
                             .ToList();

        var results = _retriever.Retrieve(docs, query, topK);
        return FormatResults(results);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // retrieve_with_feedback
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("retrieve_with_feedback")]
    [Description("Ranks text chunks against a query using BM25+, then runs RM3 pseudo-relevance feedback: expands the query with vocabulary " +
                 "from the top initial results and re-ranks. Surfaces relevant chunks that don't literally contain the query's words but share " +
                 "vocabulary with the chunks that do (e.g. a query for 'car' finding a chunk about 'Tesla battery range').")]
    public string RetrieveWithFeedback(
        [Description("Delimiter-separated list of text chunks to rank")] string documents,
        [Description("The query to rank chunks against")] string query,
        [Description("Delimiter between chunks (default: ---)")] string delimiter = "---",
        [Description("Maximum number of results to return (default: 5)")] int topK = 5,
        [Description("ISO 639-3 language code hint for function-word filtering during query expansion (default: none)")] string iso3 = "")
    {
        if (string.IsNullOrWhiteSpace(documents) || string.IsNullOrWhiteSpace(query))
            return "[empty input]";

        var docs = documents.Split(new[] { delimiter }, StringSplitOptions.None)
                             .Select(d => d.Trim())
                             .ToList();

        var results = _retriever.RetrieveWithFeedback(docs, query, topK, string.IsNullOrWhiteSpace(iso3) ? null : iso3);
        return FormatResults(results);
    }

    private static string FormatResults(List<RetrievalResult> results)
    {
        if (results.Count == 0) return "[no matches]";

        var sb = new StringBuilder();
        foreach (var r in results)
            sb.AppendLine($"[#{r.Index} | score {r.Score:F3}] {r.Document}");
        return sb.ToString().TrimEnd();
    }
}

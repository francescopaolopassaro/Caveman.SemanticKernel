// -----------------------------------------------------------------------------
// <copyright file="CavemanRouterPlugin.cs" company="Digitalsolutions.it">
//   Caveman.SemanticKernel — Semantic Kernel plugins for Caveman.
//   Copyright (c) 2026 Passaro Francesco Paolo — Digitalsolutions.it.
//   Licensed under the Caveman License (MIT + mandatory attribution).
// </copyright>
// <summary>SK plugin wrapping CavemanContentRouter (1.3.0): content-aware routing, JSON crushing, waste analysis.</summary>
// -----------------------------------------------------------------------------
using System.ComponentModel;
using System.Text;
using caveman.core;
using caveman.core.entities;
using caveman.core.services;
using Microsoft.SemanticKernel;

namespace caveman.core.SemanticKernel.Plugin;

/// <summary>
/// Semantic Kernel plugin for Caveman's content-aware compression pipeline (v1.3.0).
/// Exposes <see cref="CavemanContentRouter"/>, <see cref="CavemanJsonCrusher"/>,
/// <see cref="CavemanContentDetector"/> and <see cref="CavemanWasteAnalyzer"/> as kernel functions.
/// </summary>
public sealed class CavemanRouterPlugin
{
    private readonly CavemanContentRouter _balanced;
    private readonly CavemanContentRouter _light;
    private readonly CavemanContentRouter _agent;
    private readonly CavemanContentRouter _aggressive;
    private readonly CavemanContentDetector _detector;
    private readonly CavemanWasteAnalyzer _wasteAnalyzer;
    private readonly ITokenCounter _tokenCounter;

    public CavemanRouterPlugin()
    {
        _balanced   = CavemanContentRouter.FromProfile(CompressionProfile.Balanced);
        _light      = CavemanContentRouter.FromProfile(CompressionProfile.Light);
        _agent      = CavemanContentRouter.FromProfile(CompressionProfile.Agent);
        _aggressive = CavemanContentRouter.FromProfile(CompressionProfile.Aggressive);
        _detector   = new CavemanContentDetector();
        _wasteAnalyzer = new CavemanWasteAnalyzer();
        _tokenCounter  = new ModelTokenizer();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // route_content
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("route_content")]
    [Description("Routes any content (JSON array, log, diff, HTML, code, table, prose) through the best Caveman compressor automatically. " +
                 "Returns a summary with compressed text, content type, strategy used, and token savings. " +
                 "Profile: balanced (default), light, agent, aggressive.")]
    public async Task<string> RouteContent(
        [Description("The content to compress (any type)")] string content,
        [Description("Optional query to guide relevance scoring (e.g. 'invoice customer')")] string query = "",
        [Description("Compression profile: balanced (default), light, agent, aggressive")] string profile = "balanced")
    {
        if (string.IsNullOrWhiteSpace(content)) return "[empty input]";

        var router = profile.ToLowerInvariant() switch
        {
            "light"      => _light,
            "agent"      => _agent,
            "aggressive" => _aggressive,
            _            => _balanced
        };

        string? q = string.IsNullOrWhiteSpace(query) ? null : query;
        var result = await router.RouteAsync(content, q);

        var sb = new StringBuilder();
        sb.AppendLine($"[ContentType: {result.DetectedType}]");
        sb.AppendLine($"[Strategy: {result.StrategyUsed}]");
        sb.AppendLine($"[Tokens: {result.TokensBefore} -> {result.TokensAfter} ({result.SavingsPercent:F1}% saved)]");
        if (result.CcrHash != null)
            sb.AppendLine($"[CCR: {result.CcrHash} — dropped rows retrievable via CavemanCcrStore]");
        sb.AppendLine();
        sb.Append(result.Compressed);
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // detect_content_type
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("detect_content_type")]
    [Description("Detects the structural type of a piece of content without compressing it. " +
                 "Returns one of: JsonArray, JsonObject, Code, LogOrStacktrace, GitDiff, Html, SearchResults, Tabular, PlainText.")]
    public string DetectContentType(
        [Description("The content to classify")] string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return "PlainText";
        var r = _detector.Detect(content);
        return $"{r.Type} (confidence {r.Confidence:P0})";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // compress_json
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("compress_json")]
    [Description("Compresses a JSON array using SmartCrusher: lossless markdown-table or CSV compaction when possible, " +
                 "otherwise BM25 row-drop with a CCR marker for retrievability. " +
                 "Pass a query to improve row relevance scoring.")]
    public string CompressJson(
        [Description("A JSON array string to compress")] string jsonArray,
        [Description("Optional query to score row relevance (e.g. 'error status failed')")] string query = "",
        [Description("Maximum rows to keep in lossy mode (default 15)")] int maxItems = 15)
    {
        if (string.IsNullOrWhiteSpace(jsonArray)) return "[empty input]";

        var crusher = new CavemanJsonCrusher(CavemanCcrStore.Instance) { MaxOutputItems = maxItems };
        string? q = string.IsNullOrWhiteSpace(query) ? null : query;
        var result = crusher.Crush(jsonArray, q);

        if (!result.WasCrushed) return jsonArray;

        var sb = new StringBuilder();
        sb.AppendLine($"[Strategy: {result.Strategy} | Rows: {result.KeptRows}/{result.OriginalRows}]");
        if (result.CcrHash != null)
            sb.AppendLine($"[CCR hash: {result.CcrHash} — use retrieve_ccr to get dropped rows]");
        sb.AppendLine();
        sb.Append(result.Compressed);
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // retrieve_ccr
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("retrieve_ccr")]
    [Description("Retrieves the original rows dropped by compress_json or route_content using a CCR hash. " +
                 "Returns the full JSON of dropped rows, or an error if the hash is expired or unknown. " +
                 "CCR entries expire after 5 minutes.")]
    public string RetrieveCcr(
        [Description("The 12-character CCR hash from the <<ccr:HASH,...>> marker")] string ccrHash)
    {
        if (string.IsNullOrWhiteSpace(ccrHash)) return "[error: empty hash]";
        var json = CavemanCcrStore.Instance.Retrieve(ccrHash.Trim());
        return json ?? $"[error: CCR hash '{ccrHash}' not found or expired (TTL 5 min)]";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // analyze_waste
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("analyze_waste")]
    [Description("Analyzes content for token waste patterns without modifying it: detects HTML noise, large base64 blobs, " +
                 "excessive whitespace, and large inline JSON. Returns estimated wasted tokens per category. " +
                 "Use before compressing to understand where savings are available.")]
    public string AnalyzeWaste(
        [Description("The content to analyze for token waste")] string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return "[empty input]";

        var a = _wasteAnalyzer.Analyze(content);
        if (!a.HasWaste) return "[no waste detected]";

        var sb = new StringBuilder();
        sb.AppendLine($"Total estimated waste: {a.TotalWasteTokens} tokens");
        if (a.HtmlNoiseTokens > 0)  sb.AppendLine($"  HTML tags/comments: {a.HtmlNoiseTokens} tok");
        if (a.Base64Tokens > 0)     sb.AppendLine($"  Base64 blobs:       {a.Base64Tokens} tok");
        if (a.WhitespaceTokens > 0) sb.AppendLine($"  Excess whitespace:  {a.WhitespaceTokens} tok");
        if (a.JsonBloatTokens > 0)  sb.AppendLine($"  Large JSON blocks:  {a.JsonBloatTokens} tok");
        return sb.ToString().TrimEnd();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // batch_route
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("batch_route")]
    [Description("Routes multiple content strings (newline-separated sections) through the content-aware pipeline. " +
                 "Each section is processed independently. Returns a summary of total tokens saved.")]
    public async Task<string> BatchRoute(
        [Description("Multiple content strings separated by the delimiter")] string contents,
        [Description("Delimiter between sections (default: ---)")] string delimiter = "---",
        [Description("Compression profile: balanced, light, agent, aggressive")] string profile = "balanced")
    {
        if (string.IsNullOrWhiteSpace(contents)) return "[empty input]";

        var router = profile.ToLowerInvariant() switch
        {
            "light"      => _light,
            "agent"      => _agent,
            "aggressive" => _aggressive,
            _            => _balanced
        };

        var sections = contents.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        int totalBefore = 0, totalAfter = 0;

        for (int i = 0; i < sections.Length; i++)
        {
            var section = sections[i].Trim();
            if (string.IsNullOrEmpty(section)) continue;
            var r = await router.RouteAsync(section);
            totalBefore += r.TokensBefore;
            totalAfter  += r.TokensAfter;
            sb.AppendLine($"[Section {i + 1}: {r.DetectedType} | {r.StrategyUsed} | {r.SavingsPercent:F1}% saved]");
            sb.AppendLine(r.Compressed);
            if (i < sections.Length - 1) sb.AppendLine(delimiter);
        }

        sb.Insert(0, $"[Total: {totalBefore} -> {totalAfter} tokens ({(totalBefore > 0 ? (totalBefore - totalAfter) * 100.0 / totalBefore : 0):F1}% saved)]\n\n");
        return sb.ToString().TrimEnd();
    }
}

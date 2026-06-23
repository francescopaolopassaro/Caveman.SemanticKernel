// -----------------------------------------------------------------------------
// <copyright file="CavemanOutputPlugin.cs" company="Digitalsolutions.it">
//   Caveman.SemanticKernel — Semantic Kernel plugins for Caveman.
//   Copyright (c) 2026 Passaro Francesco Paolo — Digitalsolutions.it.
//   Licensed under the Caveman License (MIT + mandatory attribution).
// </copyright>
// <summary>SK plugin wrapping CavemanOutputShaper and CavemanCacheAligner (1.3.0): output token reduction and KV-cache protection.</summary>
// -----------------------------------------------------------------------------
using System.ComponentModel;
using System.Text;
using caveman.core.entities;
using caveman.core.services;
using Microsoft.SemanticKernel;

namespace caveman.core.SemanticKernel.Plugin;

/// <summary>
/// Semantic Kernel plugin for Caveman's output-side token reduction (v1.3.0).
/// <list type="bullet">
///   <item><see cref="CavemanOutputShaper"/> — injects verbosity-steering instructions into system prompts so the model
///   skips preambles, restatements, and rationale, saving output tokens.</item>
///   <item><see cref="CavemanCacheAligner"/> — detects volatile tokens (UUIDs, timestamps, JWTs, hex hashes) in system
///   prompts that bust the LLM provider's KV-cache prefix, causing unnecessary re-encoding costs.</item>
/// </list>
/// </summary>
public sealed class CavemanOutputPlugin
{
    private readonly CavemanOutputShaper _shaper;
    private readonly CavemanCacheAligner _aligner;

    public CavemanOutputPlugin()
    {
        _shaper  = new CavemanOutputShaper();
        _aligner = new CavemanCacheAligner();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // shape_system_prompt
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("shape_system_prompt")]
    [Description("Appends verbosity-steering instructions to a system prompt so the LLM skips preamble, restatement " +
                 "and unnecessary rationale in its responses. Reduces output tokens without any post-hoc filtering. " +
                 "The injection is byte-stable per level (good for KV-cache) and idempotent. " +
                 "Levels: 0=off, 1=skip_ceremony (no 'Sure!'), 2=no_restatement (default, also no code echoing), " +
                 "3=conclusions_only, 4=minimum_tokens (fragments OK).")]
    public string ShapeSystemPrompt(
        [Description("The system prompt to inject verbosity steering into")] string systemPrompt,
        [Description("Verbosity level 0-4 (default 2 = NoRestatement)")] int level = 2)
    {
        if (string.IsNullOrEmpty(systemPrompt)) return systemPrompt ?? string.Empty;
        level = Math.Clamp(level, 0, 4);
        return _shaper.ShapeSystemPrompt(systemPrompt, (VerbosityLevel)level);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // remove_verbosity_steering
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("remove_verbosity_steering")]
    [Description("Removes any verbosity-steering block previously injected by shape_system_prompt. " +
                 "Returns the original system prompt without the Caveman verbosity instructions.")]
    public string RemoveVerbositySteering(
        [Description("The system prompt that may contain verbosity steering")] string systemPrompt)
    {
        if (string.IsNullOrEmpty(systemPrompt)) return systemPrompt ?? string.Empty;
        return _shaper.RemoveVerbositySteering(systemPrompt);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // has_verbosity_steering
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("has_verbosity_steering")]
    [Description("Returns 'true' if the system prompt already contains Caveman verbosity-steering instructions, 'false' otherwise.")]
    public string HasVerbositySteering(
        [Description("The system prompt to check")] string systemPrompt)
    {
        if (string.IsNullOrEmpty(systemPrompt)) return "false";
        return _shaper.HasVerbositySteering(systemPrompt) ? "true" : "false";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // scan_volatile_tokens
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("scan_volatile_tokens")]
    [Description("Scans a system prompt for volatile tokens that change on every invocation and break the LLM provider's " +
                 "KV-cache prefix reuse (UUIDs, ISO-8601 timestamps, JWTs, hex hashes). " +
                 "Returns a list of findings with label and sample, or '[no volatile tokens found]'.")]
    public string ScanVolatileTokens(
        [Description("The system prompt to scan for volatile tokens")] string systemPrompt)
    {
        if (string.IsNullOrEmpty(systemPrompt)) return "[no volatile tokens found]";

        var findings = _aligner.Scan(systemPrompt);
        if (findings.Count == 0) return "[no volatile tokens found]";

        var sb = new StringBuilder();
        sb.AppendLine($"Found {findings.Count} volatile token type(s):");
        foreach (var f in findings)
            sb.AppendLine($"  {f.Label}: {f.Sample}");
        sb.AppendLine();
        sb.AppendLine("Tip: move volatile sections to the END of the system prompt to maximise KV-cache hits.");
        return sb.ToString().TrimEnd();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // has_volatile_tokens
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("has_volatile_tokens")]
    [Description("Quick check: returns 'true' if the system prompt contains any volatile tokens " +
                 "(UUIDs, timestamps, JWTs, hex hashes) that would bust the KV-cache prefix.")]
    public string HasVolatileTokens(
        [Description("The system prompt to check")] string systemPrompt)
    {
        if (string.IsNullOrEmpty(systemPrompt)) return "false";
        return _aligner.HasVolatileTokens(systemPrompt) ? "true" : "false";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // optimize_for_cache
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("optimize_for_cache")]
    [Description("One-shot system prompt optimizer: adds verbosity steering (level 2 = NoRestatement) AND reports " +
                 "any volatile tokens found. Returns the shaped prompt followed by a cache analysis section. " +
                 "Use this as the single call to prepare a system prompt for maximum KV-cache efficiency.")]
    public string OptimizeForCache(
        [Description("The system prompt to optimize")] string systemPrompt,
        [Description("Verbosity level 0-4 (default 2)")] int verbosityLevel = 2)
    {
        if (string.IsNullOrEmpty(systemPrompt)) return systemPrompt ?? string.Empty;

        verbosityLevel = Math.Clamp(verbosityLevel, 0, 4);
        var shaped = _shaper.ShapeSystemPrompt(systemPrompt, (VerbosityLevel)verbosityLevel);

        var findings = _aligner.Scan(systemPrompt);
        if (findings.Count == 0) return shaped;

        var sb = new StringBuilder(shaped);
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("<!-- caveman-cache-warning");
        sb.AppendLine($"Volatile tokens detected ({findings.Count}): " + string.Join(", ", findings.Select(f => f.Label)));
        sb.AppendLine("Move these to the END of the prompt to improve KV-cache reuse.");
        sb.AppendLine("-->");
        return sb.ToString().TrimEnd();
    }
}

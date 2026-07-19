// -----------------------------------------------------------------------------
// <copyright file="CavemanSummarizationPlugin.cs" company="Digitalsolutions.it">
//   Caveman.SemanticKernel — Semantic Kernel plugins for Caveman.
//   Copyright (c) 2026 Passaro Francesco Paolo — Digitalsolutions.it.
//   Licensed under the Caveman License (MIT + mandatory attribution).
// </copyright>
// <summary>SK plugin wrapping generic-text extractive summarization: CavemanSummarizer (TF-IDF, topic-aware) and CavemanTextRank (TextRank).</summary>
// -----------------------------------------------------------------------------
using System.ComponentModel;
using caveman.core.services;
using Microsoft.SemanticKernel;

namespace caveman.core.SemanticKernel.Plugin;

/// <summary>
/// Extractive summarization for plain text (not conversation-shaped input — see
/// <see cref="CavemanConversationPlugin"/> for role-tagged transcripts). Selects the most
/// important sentences via TF-IDF or TextRank; <see cref="SummarizeTextTopicAware"/> segments
/// the text into topics first so one dense topic can't starve the sentence budget of the rest.
/// </summary>
public sealed class CavemanSummarizationPlugin
{
    private readonly CavemanSummarizer _summarizer;
    private readonly CavemanTextRank _textRank;

    public CavemanSummarizationPlugin(CavemanSummarizer? summarizer = null, CavemanTextRank? textRank = null)
    {
        _summarizer = summarizer ?? new CavemanSummarizer(new FunctionWordProvider());
        _textRank = textRank ?? new CavemanTextRank(new FunctionWordProvider());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // summarize_text
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("summarize_text")]
    [Description("Extractive summarization of plain text — selects the most important sentences using TF-IDF or TextRank. Keeps the meaning, discards redundancy.")]
    public string SummarizeText(
        [Description("Text to summarize")] string text,
        [Description("Number of sentences to keep (overrides ratio when set)")] int? sentences = null,
        [Description("Fraction of sentences to keep, e.g. 0.3 for 30% (default: 0.3)")] float ratio = 0.3f,
        [Description("Algorithm: tfidf or textrank (default: textrank)")] string algo = "textrank",
        [Description("ISO 639-3 language code hint (default: auto-detect)")] string iso3 = "")
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string? iso = string.IsNullOrWhiteSpace(iso3) ? null : iso3;

        if (algo.Equals("tfidf", StringComparison.OrdinalIgnoreCase))
        {
            return sentences.HasValue
                ? _summarizer.Summarize(text, sentences.Value, iso)
                : _summarizer.Summarize(text, ratio, iso);
        }

        return sentences.HasValue
            ? _textRank.Summarize(text, sentences.Value)
            : _textRank.Summarize(text, ratio);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // summarize_text_topic_aware
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("summarize_text_topic_aware")]
    [Description("Extractive summarization that first segments the text into topics (TextTiling-style), then allocates the sentence budget " +
                 "proportionally across them. Prevents a single statistically dense topic from dominating and starving the others — use over " +
                 "summarize_text for long, multi-topic documents.")]
    public string SummarizeTextTopicAware(
        [Description("Text to summarize")] string text,
        [Description("Number of sentences to keep (default: 5)")] int sentences = 5,
        [Description("ISO 639-3 language code hint (default: auto-detect)")] string iso3 = "")
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return _summarizer.CondenseTextTopicAware(text, sentences, string.IsNullOrWhiteSpace(iso3) ? null : iso3);
    }
}

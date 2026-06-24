// -----------------------------------------------------------------------------
// <copyright file="CavemanConversationPlugin.cs" company="Digitalsolutions.it">
//   Caveman — NLP prompt compressor for LLMs.
//   Copyright (c) 2026 Passaro Francesco Paolo — Digitalsolutions.it.
//   Licensed under the Caveman License (MIT + mandatory attribution): any use
//   must disclose use of the Caveman library by Passaro Francesco Paolo
//   (Digitalsolutions.it). See the LICENSE file for full terms.
// </copyright>
// <summary>Semantic Kernel plugin exposing Caveman conversation/agent-memory tools to the model.</summary>
// -----------------------------------------------------------------------------
using System.ComponentModel;
using System.Text;
using caveman.core.services;
using Microsoft.SemanticKernel;

namespace caveman.core.SemanticKernel.Plugin;

/// <summary>
/// Conversation-level tools an AI agent can call to manage its own context: summarize a whole
/// transcript, fit it to a token budget, distill a durable memory, focus on a query, estimate
/// token usage, share compressed context across agents, and detect/remove duplicate messages.
/// All operations are local (no embeddings, no extra LLM calls).
/// </summary>
public class CavemanConversationPlugin
{
    private readonly CavemanTextRank _textRank;
    private readonly CavemanMemoryExtractor _memory;
    private readonly CavemanRelevanceFilter _relevance;
    private readonly ModelTokenizer _tokenizer;
    private readonly CavemanSharedContext _sharedContext;
    private readonly CavemanMessageDeduplicator _deduplicator;

    public CavemanConversationPlugin(
        CavemanTextRank? textRank = null,
        CavemanMemoryExtractor? memory = null,
        CavemanRelevanceFilter? relevance = null,
        CavemanSharedContext? sharedContext = null,
        CavemanMessageDeduplicator? deduplicator = null)
    {
        _textRank = textRank ?? new CavemanTextRank();
        _memory = memory ?? new CavemanMemoryExtractor();
        _relevance = relevance ?? new CavemanRelevanceFilter();
        _tokenizer = new ModelTokenizer();
        _sharedContext = sharedContext ?? CavemanSharedContext.Instance;
        _deduplicator = deduplicator ?? new CavemanMessageDeduplicator();
    }

    [KernelFunction("summarize_conversation")]
    [Description("Cleans a full chatbot/LLM conversation (markdown/JSON/HTML) and summarizes only the long natural-language passages, keeping service results and keyword lists verbatim. Set parseRoles=true for role-tagged transcripts.")]
    public string SummarizeConversation(
        [Description("The whole conversation context")] string conversation,
        [Description("Parse roles/turns (OpenAI/Anthropic JSON, ChatML, transcripts)")] bool parseRoles = true,
        [Description("Keep the last N turns verbatim (recency window)")] int keepLastTurns = 0)
    {
        return _textRank.RankAndSummarizeChat(conversation, new ChatSummarizeOptions
        {
            ParseConversation = parseRoles,
            KeepLastTurnsVerbatim = keepLastTurns
        });
    }

    [KernelFunction("fit_to_budget")]
    [Description("Compresses a conversation so it fits within a hard token budget for the given model, summarizing and (as needed) dropping the oldest turns. Returns the text that fits.")]
    public string FitToBudget(
        [Description("The whole conversation context")] string conversation,
        [Description("Maximum tokens the result must fit within")] int maxTokens,
        [Description("Model: gpt-4 (default), gpt-3.5, llama-3, gemma-3, claude-3")] string model = "gpt-4",
        [Description("Keep the last N turns verbatim")] int keepLastTurns = 4)
    {
        var result = _textRank.RankAndSummarizeChatDetailed(conversation, new ChatSummarizeOptions
        {
            ParseConversation = true,
            KeepLastTurnsVerbatim = keepLastTurns,
            KeepSystemVerbatim = true,
            MaxTokens = maxTokens,
            TokenModel = ParseModel(model)
        });
        return result.Text;
    }

    [KernelFunction("extract_memory")]
    [Description("Distills a durable memory from a conversation: the most salient sentences plus key terms/entities. Use to remember what matters while forgetting the raw transcript.")]
    public string ExtractMemory(
        [Description("The conversation or text to remember")] string conversation,
        [Description("Max salient sentences to keep")] int maxSentences = 5,
        [Description("Max key terms to keep")] int maxKeywords = 10)
    {
        return _memory.Extract(conversation, maxSentences, maxKeywords).ToString();
    }

    [KernelFunction("focus_conversation")]
    [Description("Query-focused context shaping: keeps only the blocks most relevant to a question (embedding-free lexical relevance). Use to shrink a large context to what matters for the current query.")]
    public string FocusConversation(
        [Description("The large context/conversation to filter")] string conversation,
        [Description("The question or topic to focus on")] string query,
        [Description("How many top blocks to keep")] int topK = 5)
    {
        return _relevance.Focus(conversation, query, topK);
    }

    [KernelFunction("estimate_tokens")]
    [Description("Estimates the token count of a text for a given model. Useful to check context-window usage before sending.")]
    public string EstimateTokens(
        [Description("The text to measure")] string text,
        [Description("Model: gpt-4 (default), gpt-3.5, llama-3, gemma-3, claude-3")] string model = "gpt-4")
    {
        int tokens = _tokenizer.CountTokens(text ?? string.Empty, ParseModel(model));
        return tokens.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // share_context  (CavemanSharedContext)
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("share_context")]
    [Description("Compresses content using Caveman NLP compression and stores it in the inter-agent shared context store under the given key (TTL 30 min). " +
                 "Returns token savings metadata. Agents can then call get_shared_context with the same key to retrieve the compressed copy, " +
                 "saving tokens on every read.")]
    public string ShareContext(
        [Description("Unique key identifying this context entry (e.g. 'user-profile', 'search-results')")] string key,
        [Description("The content to compress and share")] string content,
        [Description("Optional name of the agent storing the context (for bookkeeping)")] string agentName = "")
    {
        if (string.IsNullOrWhiteSpace(key)) return "[error: key is required]";
        if (string.IsNullOrWhiteSpace(content)) return "[error: content is required]";

        var entry = _sharedContext.Put(key, content, string.IsNullOrWhiteSpace(agentName) ? null : agentName);
        double pct = entry.TokensBefore > 0 ? (double)entry.TokensSaved / entry.TokensBefore * 100 : 0;
        return $"[SharedContext: stored '{key}' | {entry.TokensBefore} → {entry.TokensAfter} tokens ({pct:F1}% saved) | TTL 30 min]";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // get_shared_context  (CavemanSharedContext)
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("get_shared_context")]
    [Description("Retrieves a context entry from the inter-agent shared store by key. " +
                 "By default returns the compressed copy (fewer tokens); pass full=true to get the original. " +
                 "Returns an error message if the key is unknown or expired (TTL 30 min).")]
    public string GetSharedContext(
        [Description("The key used when storing the context via share_context")] string key,
        [Description("Set to true to retrieve the original uncompressed content")] bool full = false)
    {
        if (string.IsNullOrWhiteSpace(key)) return "[error: key is required]";
        var result = _sharedContext.Get(key.Trim(), full);
        return result ?? $"[error: shared context key '{key}' not found or expired (TTL 30 min)]";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // shared_context_stats  (CavemanSharedContext)
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("shared_context_stats")]
    [Description("Returns aggregate statistics for all active entries in the inter-agent shared context store: " +
                 "entry count, total tokens before/after compression, and total tokens saved.")]
    public string SharedContextStats()
    {
        var (entries, before, after, saved) = _sharedContext.Stats;
        if (entries == 0) return "[SharedContext: no active entries]";
        double pct = before > 0 ? (double)saved / before * 100 : 0;
        return $"[SharedContext: {entries} entries | {before} → {after} tokens | {saved} saved ({pct:F1}%)]";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // dedup_messages  (CavemanMessageDeduplicator)
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("dedup_messages")]
    [Description("Scans a sequence of conversation messages (separated by a delimiter) for duplicate content. " +
                 "A message is a duplicate when it re-appears more than 3 positions after the original (re-reads waste tokens). " +
                 "Returns a report with duplicate pairs and estimated wasted tokens, or '[no duplicates found]'.")]
    public string DedupMessages(
        [Description("Newline-delimited (or custom-delimited) list of message content strings to scan")] string messages,
        [Description("Delimiter between messages (default: ---)")] string delimiter = "---")
    {
        if (string.IsNullOrWhiteSpace(messages)) return "[no duplicates found]";

        var parts = messages.Split(new[] { delimiter }, StringSplitOptions.None)
                            .Select(m => m.Trim())
                            .ToList();

        var result = _deduplicator.FindDuplicates(parts);
        if (!result.HasDuplicates) return "[no duplicates found]";

        var sb = new StringBuilder();
        sb.AppendLine($"Found {result.DuplicatePairs.Count} duplicate(s) — ~{result.EstimatedWastedTokens} tokens wasted:");
        foreach (var (orig, dup) in result.DuplicatePairs)
            sb.AppendLine($"  Message #{dup + 1} is a duplicate of message #{orig + 1}");
        sb.Append("Use clean_duplicate_messages to replace duplicates with placeholders.");
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // clean_duplicate_messages  (CavemanMessageDeduplicator)
    // ─────────────────────────────────────────────────────────────────────────

    [KernelFunction("clean_duplicate_messages")]
    [Description("Removes duplicate messages from a conversation sequence (separated by a delimiter): " +
                 "replaces each duplicate with a short '[duplicate of message #N]' placeholder. " +
                 "Preserves message count and order. Returns the cleaned sequence using the same delimiter.")]
    public string CleanDuplicateMessages(
        [Description("Newline-delimited (or custom-delimited) list of message content strings")] string messages,
        [Description("Delimiter between messages (default: ---)")] string delimiter = "---")
    {
        if (string.IsNullOrWhiteSpace(messages)) return messages ?? string.Empty;

        var parts = messages.Split(new[] { delimiter }, StringSplitOptions.None)
                            .Select(m => m.Trim())
                            .ToList();

        var cleaned = _deduplicator.RemoveDuplicates(parts);
        return string.Join($"\n{delimiter}\n", cleaned);
    }

    private static LlmModel ParseModel(string model) => (model ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "gpt-3.5" or "gpt-3.5-turbo" or "gpt35" => LlmModel.Gpt3_5Turbo,
        "llama-3" or "llama3" or "llama" => LlmModel.Llama3,
        "gemma-3" or "gemma3" or "gemma" => LlmModel.Gemma3,
        "claude-3" or "claude3" or "claude" => LlmModel.Claude3,
        _ => LlmModel.Gpt4
    };
}

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
using caveman.core.services;
using Microsoft.SemanticKernel;

namespace caveman.core.SemanticKernel.Plugin;

/// <summary>
/// Conversation-level tools an AI agent can call to manage its own context: summarize a whole
/// transcript, fit it to a token budget, distill a durable memory, focus on a query, or estimate
/// token usage. All operations are local (no embeddings, no extra LLM calls).
/// </summary>
public class CavemanConversationPlugin
{
    private readonly CavemanTextRank _textRank;
    private readonly CavemanMemoryExtractor _memory;
    private readonly CavemanRelevanceFilter _relevance;
    private readonly ModelTokenizer _tokenizer;

    public CavemanConversationPlugin(
        CavemanTextRank? textRank = null,
        CavemanMemoryExtractor? memory = null,
        CavemanRelevanceFilter? relevance = null)
    {
        _textRank = textRank ?? new CavemanTextRank();
        _memory = memory ?? new CavemanMemoryExtractor();
        _relevance = relevance ?? new CavemanRelevanceFilter();
        _tokenizer = new ModelTokenizer();
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

    private static LlmModel ParseModel(string model) => (model ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "gpt-3.5" or "gpt-3.5-turbo" or "gpt35" => LlmModel.Gpt3_5Turbo,
        "llama-3" or "llama3" or "llama" => LlmModel.Llama3,
        "gemma-3" or "gemma3" or "gemma" => LlmModel.Gemma3,
        "claude-3" or "claude3" or "claude" => LlmModel.Claude3,
        _ => LlmModel.Gpt4
    };
}

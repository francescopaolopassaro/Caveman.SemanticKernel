// -----------------------------------------------------------------------------
// <copyright file="TokenOptimizerPlugin.cs" company="Digitalsolutions.it">
//   Caveman — NLP prompt compressor for LLMs.
//   Copyright (c) 2026 Passaro Francesco Paolo — Digitalsolutions.it.
//   Licensed under the Caveman License (MIT + mandatory attribution): any use
//   must disclose use of the Caveman library by Passaro Francesco Paolo
//   (Digitalsolutions.it). See the LICENSE file for full terms.
// </copyright>
// <summary>Semantic Kernel plugin exposing prompt compression as a kernel function.</summary>
// -----------------------------------------------------------------------------
/*---------------------------------------------------------------------------------------
 * PROJECT: Caveman (NLP Prompt Compressor) - Semantic Kernel Plugin
 * DESCRIPTION:
 * Plugin per Semantic Kernel che espone la logica di compressione Caveman come funzione 
 * invocabile dall'IA. Ottimizza i prompt riducendo i token mantenendo il significato.
 * Include graceful degradation per input non supportati dal modello NLP.
 * 
 * AUTHOR: [Francesco Paolo Passaro]
 * DATE: May 2026
 *---------------------------------------------------------------------------------------*/


using System.ComponentModel;
using Microsoft.SemanticKernel;
using caveman.core.entities;
using caveman.core.services;

namespace caveman.core.SemanticKernel.Plugin
{
    /// <summary>
    /// Plugin Semantic Kernel per l'ottimizzazione token di prompt testuali.
    /// Gestisce automaticamente fallback sicuri per input non processabili (emoji, lingue non supportate, ecc.).
    /// </summary>
    public class TokenOptimizerPlugin
    {
        private readonly CavemanCompressionService _compressionService;
        private readonly ModelTokenizer _tokenizer = new();

        /// <summary>
        /// Inizializza il plugin con il servizio di compressione fornito.
        /// </summary>
        /// <param name="compressionService">Istanza di CavemanCompressionService.</param>
        public TokenOptimizerPlugin(CavemanCompressionService compressionService)
        {
            _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));
        }

        [KernelFunction("idf_languages")]
        [Description("Reports the 56 per-language global IDF tables shipped with v1.4.4 (used by the statistical level's 50/50 global-IDF blend and the aggressive ubiquity gate). When iso3 is given, returns whether a table exists for it and its reference-corpus size.")]
        public string IdfLanguages(
            [Description("ISO 639-3 code to check for a shipped global-IDF table (optional)")] string? iso3 = null)
        {
            var provider = new CavemanGlobalIdfProvider();
            var languages = CavemanGlobalIdfProvider.GetShippedLanguages().OrderBy(l => l).ToList();

            if (!string.IsNullOrWhiteSpace(iso3))
            {
                var code = iso3.Trim().ToLowerInvariant();
                return provider.HasData(code)
                    ? $"{code}: available (corpus {provider.GetCorpusSize(code).ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} documents)"
                    : $"{code}: no shipped table";
            }

            return $"language_count: {languages.Count}\n" + string.Join(", ", languages);
        }

        /// <summary>
        /// Stima il numero di token di un testo per il modello indicato, utile per verificare
        /// l'occupazione della finestra di contesto prima dell'invio.
        /// </summary>
        [KernelFunction("estimate_tokens")]
        [Description("Estimates the token count of a text for a given model family (gpt, claude, gemini, gemma, llama, qwen, deepseek, grok, glm, spark, mistral, nova, kimi, ernie, command).")]
        public int EstimateTokens(
            [Description("The text to measure")] string input,
            [Description("Model name, any version: gpt-4 (default), claude-sonnet, gemini-2.5-pro, qwen2.5-72b, deepseek-v3, grok-4, glm-4.6, ...")] string model = "gpt-4")
        {
            var llm = LlmModelParser.Parse(model);
            return _tokenizer.CountTokens(input ?? string.Empty, llm);
        }

        /// <summary>
        /// Comprime il testo in input riducendo i token secondo il livello specificato.
        /// Se l'input non è supportato dal motore NLP (es. solo emoji/simboli), restituisce 
        /// il testo originale senza errori per non interrompere il flusso dell'agente AI.
        /// </summary>
        /// <param name="input">Testo originale da ottimizzare.</param>
        /// <param name="level">Livello di compressione: 0=Nessuno, 1=Light, 2=Semantico, 3=Aggressivo.</param>
        /// <returns>Risultato della compressione con metriche dettagliate.</returns>
        [KernelFunction("OptimizePrompt")]
        [Description("Optimizes the prompt by reducing tokens based on the required level (0-5). " +
                     "Returns CompressionResult with compressed text and green metrics. " +
                     "Gracefully falls back to original text if language/model is unsupported.")]
        public async Task<CompressionResult> OptimizePrompt(
            [Description("The raw text to compress")] string input,
            [Description("Level: 0=None, 1=Light, 2=Semantic (default), 3=Aggressive, 4=Statistical (TF-IDF), 5=Syntactic (rule-based, grammatical)")] int level = 2)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            int safeLevel = Math.Clamp(level, 0, 5);

            try
            {
                // Tentativo di compressione standard
                return await _compressionService.CompressAsync(input, (CavemanCompressionLevel)safeLevel);
            }
            catch (NotSupportedException)
            {
                //  Graceful degradation: il modello NLP non supporta questa lingua/tipo di input
                // Restituiamo il testo originale per non rompere il flusso dell'AI
                return new CompressionResult
                {
                    CompressedText = input,
                    OriginalTokens = 0,
                    CompressedTokens = 0
                };
            }
            catch (Exception)
            {
                // Fallback ultimo per qualsiasi altro errore imprevisto del servizio
                return new CompressionResult
                {
                    CompressedText = input,
                    OriginalTokens = 0,
                    CompressedTokens = 0
                };
            }
        }
    }
}
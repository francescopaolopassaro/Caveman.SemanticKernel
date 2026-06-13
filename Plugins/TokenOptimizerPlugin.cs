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

        /// <summary>
        /// Stima il numero di token di un testo per il modello indicato, utile per verificare
        /// l'occupazione della finestra di contesto prima dell'invio.
        /// </summary>
        [KernelFunction("estimate_tokens")]
        [Description("Estimates the token count of a text for a given model (gpt-4, gpt-3.5, llama-3, gemma-3, claude-3).")]
        public int EstimateTokens(
            [Description("The text to measure")] string input,
            [Description("Model: gpt-4 (default), gpt-3.5, llama-3, gemma-3, claude-3")] string model = "gpt-4")
        {
            var llm = (model ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "gpt-3.5" or "gpt-3.5-turbo" or "gpt35" => LlmModel.Gpt3_5Turbo,
                "llama-3" or "llama3" or "llama" => LlmModel.Llama3,
                "gemma-3" or "gemma3" or "gemma" => LlmModel.Gemma3,
                "claude-3" or "claude3" or "claude" => LlmModel.Claude3,
                _ => LlmModel.Gpt4
            };
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
        [Description("Optimizes the prompt by reducing tokens based on the required level (0-3). " +
                     "Returns CompressionResult with compressed text and green metrics. " +
                     "Gracefully falls back to original text if language/model is unsupported.")]
        public async Task<CompressionResult> OptimizePrompt(
            [Description("The raw text to compress")] string input,
            [Description("Level: 0=None, 1=Light, 2=Semantic (default), 3=Aggressive")] int level = 2)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            int safeLevel = Math.Clamp(level, 0, 3);

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
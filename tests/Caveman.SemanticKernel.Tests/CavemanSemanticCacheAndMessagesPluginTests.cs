using caveman.core;
using System.Text.Json;
using caveman.core.services;
using caveman.core.SemanticKernel.Plugin;

namespace caveman.tests;

[TestFixture]
public class CavemanSemanticCacheAndMessagesPluginTests
{
    private static CavemanSemanticCachePlugin NewPlugin() =>
        new(new CavemanSemanticCache(new caveman.core.entities.CavemanSemanticCacheOptions { Language = "ita" }));

    [Test]
    public void SemanticCache_ParaphraseIsHit_DifferentFileIsMiss()
    {
        var p = NewPlugin();
        p.Set("Come posso creare un nuovo file di configurazione per il database?", "usa il comando init");

        using var hit = JsonDocument.Parse(p.Get("Come creare un nuovo file configurazione database?"));
        Assert.That(hit.RootElement.GetProperty("hit").GetBoolean(), Is.True);
        Assert.That(hit.RootElement.GetProperty("response").GetString(), Is.EqualTo("usa il comando init"));

        p.Set("Elimina il file Program.cs dal progetto", "ok");
        using var miss = JsonDocument.Parse(p.Get("Elimina il file Startup.cs dal progetto"));
        Assert.That(miss.RootElement.GetProperty("hit").GetBoolean(), Is.False);
    }

    [Test]
    public void SemanticCache_StatsAndClear()
    {
        var p = NewPlugin();
        p.Set("alpha beta gamma", "x");
        p.Get("alpha beta gamma");
        using var stats = JsonDocument.Parse(p.Stats());
        Assert.That(stats.RootElement.GetProperty("hits").GetInt64(), Is.EqualTo(1));
        p.Clear();
        using var after = JsonDocument.Parse(p.Stats());
        Assert.That(after.RootElement.GetProperty("entries").GetInt32(), Is.EqualTo(0));
    }

    private const string ToolChat =
        "[{\"role\":\"system\",\"content\":\"Sei utile.\"}," +
        "{\"role\":\"user\",\"content\":\"meteo a Roma?\"}," +
        "{\"role\":\"assistant\",\"content\":null,\"tool_calls\":[{\"id\":\"call_9\",\"type\":\"function\",\"function\":{\"name\":\"get_weather\",\"arguments\":\"{\\\"city\\\":\\\"Roma\\\"}\"}}]}," +
        "{\"role\":\"tool\",\"tool_call_id\":\"call_9\",\"content\":\"{\\\"temp\\\":30}\"}," +
        "{\"role\":\"assistant\",\"content\":\"A Roma ci sono 30 gradi.\"}]";

    [Test]
    public void CompactMessages_OpenAiFormat_KeepsToolCallsAndResults()
    {
        var json = new CavemanConversationPlugin().CompactMessages(ToolChat, maxTokens: 25, keepLastTurns: 1);
        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement.GetRawText();
        Assert.That(text, Does.Contain("call_9"));
        Assert.That(text, Does.Contain("get_weather"));
        Assert.That(text, Does.Contain("tool_call_id"));
    }

    [Test]
    public void CompactMessages_ConverseFormat_IsStrictlyAlternatingWithSeparateSystem()
    {
        var json = new CavemanConversationPlugin().CompactMessages(ToolChat, format: "converse", model: "us.amazon.nova-pro-v1:0");
        using var doc = JsonDocument.Parse(json);

        Assert.That(doc.RootElement.GetProperty("system").GetString(), Is.EqualTo("Sei utile."));
        var roles = doc.RootElement.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("role").GetString()).ToList();
        Assert.That(roles.First(), Is.EqualTo("user"));
        for (int i = 1; i < roles.Count; i++) Assert.That(roles[i], Is.Not.EqualTo(roles[i - 1]));
        Assert.That(doc.RootElement.GetRawText(), Does.Contain("toolUse").And.Contain("toolResult"));
    }

    [TestCase("gemini-2.5-pro", "Gemini")]
    [TestCase("qwen2.5-72b-instruct", "Qwen")]
    [TestCase("deepseek-r1", "DeepSeek")]
    [TestCase("grok-4", "Grok")]
    [TestCase("glm-4.6", "Glm")]
    [TestCase("spark-max", "Spark")]
    [TestCase("gpt-4o", "Gpt4")]
    [TestCase("gpt-3.5-turbo", "Gpt3_5Turbo")]
    [TestCase("claude-sonnet-4", "Claude3")]
    [TestCase("us.amazon.nova-pro-v1:0", "Nova")]
    [TestCase("mistral-large", "Mistral")]
    [TestCase("unknown-model", "Gpt4")]
    public void ModelParser_RecognisesMajorFamilies(string name, string expected)
        => Assert.That(LlmModelParser.Parse(name).ToString(), Is.EqualTo(expected));

    [Test]
    public void EstimateTokens_AcceptsAnyMajorModelName()
    {
        var p = new TokenOptimizerPlugin(new CavemanCompressionService());
        foreach (var m in new[] { "gemini-2.5-pro", "qwen2.5", "deepseek-v3", "grok-4", "glm-4.6", "spark-max", "kimi-k2" })
            Assert.That(p.EstimateTokens("Nel piccolo villaggio viveva un uomo di nome Elia", m), Is.GreaterThan(0), m);
    }
}

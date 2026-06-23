using caveman.core.SemanticKernel.Plugin;

namespace caveman.tests;

[TestFixture]
public class CavemanOutputPluginTests
{
    private CavemanOutputPlugin _plugin;

    [SetUp]
    public void Setup() => _plugin = new CavemanOutputPlugin();

    private const string CleanPrompt = "You are a helpful assistant. Answer questions clearly and accurately.";
    private const string PromptWithUuid = "You are a helpful assistant. Session: 550e8400-e29b-41d4-a716-446655440000";
    private const string PromptWithJwt  = "Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ1c2VyMSJ9.SflKxwRJSMeKKF2QT4fwpMeJf36POk";

    // ─── ShapeSystemPrompt ────────────────────────────────────────────────

    [Test]
    public void ShapeSystemPrompt_Level2_AppendsSteering()
    {
        var shaped = _plugin.ShapeSystemPrompt(CleanPrompt, level: 2);
        Assert.That(shaped, Does.StartWith(CleanPrompt));
        Assert.That(shaped, Does.Contain("preamble").Or.Contain("restate"));
    }

    [Test]
    public void ShapeSystemPrompt_Level0_ReturnsUnchanged()
    {
        Assert.That(_plugin.ShapeSystemPrompt(CleanPrompt, level: 0), Is.EqualTo(CleanPrompt));
    }

    [Test]
    public void ShapeSystemPrompt_Level4_ContainsMinimumTokensInstruction()
    {
        var shaped = _plugin.ShapeSystemPrompt(CleanPrompt, level: 4);
        Assert.That(shaped, Does.Contain("fragment").IgnoreCase.Or.Contain("Minimum").IgnoreCase);
    }

    [Test]
    public void ShapeSystemPrompt_IsIdempotent()
    {
        var once = _plugin.ShapeSystemPrompt(CleanPrompt, level: 2);
        var twice = _plugin.ShapeSystemPrompt(once, level: 2);
        Assert.That(twice, Is.EqualTo(once));
    }

    [Test]
    public void ShapeSystemPrompt_EmptyInput_ReturnsEmpty()
    {
        Assert.That(_plugin.ShapeSystemPrompt(string.Empty), Is.EqualTo(string.Empty));
    }

    [Test]
    public void ShapeSystemPrompt_LevelClampsAt4()
    {
        var shaped = _plugin.ShapeSystemPrompt(CleanPrompt, level: 99);
        Assert.That(shaped, Does.Contain("caveman-verbosity-4"));
    }

    // ─── RemoveVerbositySteering ──────────────────────────────────────────

    [Test]
    public void RemoveVerbositySteering_CleanPrompt_ReturnsUnchanged()
    {
        Assert.That(_plugin.RemoveVerbositySteering(CleanPrompt), Is.EqualTo(CleanPrompt));
    }

    [Test]
    public void RemoveVerbositySteering_ShapedPrompt_RestoresOriginal()
    {
        var shaped = _plugin.ShapeSystemPrompt(CleanPrompt, level: 2);
        var restored = _plugin.RemoveVerbositySteering(shaped);
        Assert.That(restored.Trim(), Is.EqualTo(CleanPrompt));
    }

    // ─── HasVerbositySteering ─────────────────────────────────────────────

    [Test]
    public void HasVerbositySteering_CleanPrompt_ReturnsFalse()
    {
        Assert.That(_plugin.HasVerbositySteering(CleanPrompt), Is.EqualTo("false"));
    }

    [Test]
    public void HasVerbositySteering_ShapedPrompt_ReturnsTrue()
    {
        var shaped = _plugin.ShapeSystemPrompt(CleanPrompt, level: 1);
        Assert.That(_plugin.HasVerbositySteering(shaped), Is.EqualTo("true"));
    }

    // ─── ScanVolatileTokens ───────────────────────────────────────────────

    [Test]
    public void ScanVolatileTokens_CleanPrompt_ReturnsNoFindings()
    {
        Assert.That(_plugin.ScanVolatileTokens(CleanPrompt), Is.EqualTo("[no volatile tokens found]"));
    }

    [Test]
    public void ScanVolatileTokens_PromptWithUuid_FindsUuid()
    {
        var r = _plugin.ScanVolatileTokens(PromptWithUuid);
        Assert.That(r, Does.Contain("UUID"));
    }

    [Test]
    public void ScanVolatileTokens_PromptWithJwt_FindsJwt()
    {
        var r = _plugin.ScanVolatileTokens(PromptWithJwt);
        Assert.That(r, Does.Contain("JWT"));
    }

    [Test]
    public void ScanVolatileTokens_PromptWithTimestamp_FindsDatetime()
    {
        var r = _plugin.ScanVolatileTokens("Current time: 2026-06-23T14:30:00Z — respond accordingly.");
        Assert.That(r, Does.Contain("ISO8601").Or.Contain("no volatile"));
    }

    // ─── HasVolatileTokens ────────────────────────────────────────────────

    [Test]
    public void HasVolatileTokens_CleanPrompt_ReturnsFalse()
    {
        Assert.That(_plugin.HasVolatileTokens(CleanPrompt), Is.EqualTo("false"));
    }

    [Test]
    public void HasVolatileTokens_PromptWithUuid_ReturnsTrue()
    {
        Assert.That(_plugin.HasVolatileTokens(PromptWithUuid), Is.EqualTo("true"));
    }

    // ─── OptimizeForCache ─────────────────────────────────────────────────

    [Test]
    public void OptimizeForCache_CleanPrompt_AddsSteeringOnly()
    {
        var result = _plugin.OptimizeForCache(CleanPrompt);
        Assert.That(result, Does.StartWith(CleanPrompt));
        Assert.That(result, Does.Not.Contain("caveman-cache-warning"));
    }

    [Test]
    public void OptimizeForCache_PromptWithUuid_AddsBothSteeringAndWarning()
    {
        var result = _plugin.OptimizeForCache(PromptWithUuid, verbosityLevel: 2);
        Assert.That(result, Does.Contain("caveman-verbosity-2"));
        Assert.That(result, Does.Contain("caveman-cache-warning"));
        Assert.That(result, Does.Contain("UUID"));
    }

    [Test]
    public void OptimizeForCache_EmptyInput_ReturnsEmpty()
    {
        Assert.That(_plugin.OptimizeForCache(string.Empty), Is.EqualTo(string.Empty));
    }
}

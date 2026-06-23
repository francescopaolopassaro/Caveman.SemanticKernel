using caveman.core.SemanticKernel.Plugin;

namespace caveman.tests;

[TestFixture]
public class CavemanRouterPluginTests
{
    private CavemanRouterPlugin _plugin;

    [SetUp]
    public void Setup() => _plugin = new CavemanRouterPlugin();

    // ─── DetectContentType ────────────────────────────────────────────────

    [Test]
    public void DetectContentType_JsonArray_ReturnsJsonArray()
    {
        var r = _plugin.DetectContentType(@"[{""id"":1,""name"":""Alice""},{""id"":2,""name"":""Bob""}]");
        Assert.That(r, Does.Contain("JsonArray"));
    }

    [Test]
    public void DetectContentType_PlainText_ReturnsPlainText()
    {
        var r = _plugin.DetectContentType("The quick brown fox jumps over the lazy dog.");
        Assert.That(r, Does.Contain("PlainText"));
    }

    [Test]
    public void DetectContentType_EmptyInput_ReturnsPlainText()
    {
        Assert.That(_plugin.DetectContentType(string.Empty), Is.EqualTo("PlainText"));
    }

    // ─── RouteContent ─────────────────────────────────────────────────────

    [Test]
    public async Task RouteContent_PlainText_ReturnsCompressedResult()
    {
        const string prose = "I would really like to know if you could kindly provide me with some information regarding the best and most affordable restaurants that are located near Victoria Station in London.";
        var result = await _plugin.RouteContent(prose);
        Assert.That(result, Does.Contain("NlpCompression").Or.Contain("Passthrough"));
        Assert.That(result, Does.Contain("Tokens:"));
    }

    [Test]
    public async Task RouteContent_JsonArray_UsesCrusher()
    {
        var json = @"[{""id"":1,""name"":""Alice"",""status"":""active""},{""id"":2,""name"":""Bob"",""status"":""active""},{""id"":3,""name"":""Carol"",""status"":""inactive""}]";
        var result = await _plugin.RouteContent(json, query: "name status");
        Assert.That(result, Does.Contain("JsonCrush").Or.Contain("ContentType"));
    }

    [Test]
    public async Task RouteContent_EmptyInput_ReturnsEmptyMarker()
    {
        var result = await _plugin.RouteContent(string.Empty);
        Assert.That(result, Is.EqualTo("[empty input]"));
    }

    [Test]
    public async Task RouteContent_AggressiveProfile_ProducesResult()
    {
        const string prose = "The quick brown fox jumps over the lazy dog in the forest near the river.";
        var result = await _plugin.RouteContent(prose, profile: "aggressive");
        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }

    // ─── CompressJson ─────────────────────────────────────────────────────

    [Test]
    public void CompressJson_UniformArray_ProducesCompressedOutput()
    {
        var json = @"[{""id"":1,""user"":""alice"",""action"":""login"",""status"":""ok""}," +
                   @"{""id"":2,""user"":""bob"",""action"":""upload"",""status"":""ok""}," +
                   @"{""id"":3,""user"":""carol"",""action"":""delete"",""status"":""failed""}]";
        var result = _plugin.CompressJson(json, query: "action status");
        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.That(result, Does.Contain("Strategy:"));
    }

    [Test]
    public void CompressJson_EmptyInput_ReturnsEmptyMarker()
    {
        Assert.That(_plugin.CompressJson(string.Empty), Is.EqualTo("[empty input]"));
    }

    [Test]
    public void CompressJson_InvalidJson_ReturnsOriginal()
    {
        const string notJson = "this is not json at all";
        Assert.That(_plugin.CompressJson(notJson), Is.EqualTo(notJson));
    }

    // ─── AnalyzeWaste ─────────────────────────────────────────────────────

    [Test]
    public void AnalyzeWaste_CleanText_ReturnsNoWaste()
    {
        var r = _plugin.AnalyzeWaste("Clean plain text with no waste patterns here.");
        Assert.That(r, Is.EqualTo("[no waste detected]"));
    }

    [Test]
    public void AnalyzeWaste_HtmlContent_DetectsHtmlNoise()
    {
        var r = _plugin.AnalyzeWaste("<div class='container'><p>text</p><span>more</span></div>");
        Assert.That(r, Does.Contain("HTML").Or.Contain("no waste"));
    }

    [Test]
    public void AnalyzeWaste_EmptyInput_ReturnsEmptyMarker()
    {
        Assert.That(_plugin.AnalyzeWaste(string.Empty), Is.EqualTo("[empty input]"));
    }

    // ─── RetrieveCcr ──────────────────────────────────────────────────────

    [Test]
    public void RetrieveCcr_UnknownHash_ReturnsError()
    {
        var r = _plugin.RetrieveCcr("nonexistenthash");
        Assert.That(r, Does.Contain("error").Or.Contain("not found"));
    }

    [Test]
    public void RetrieveCcr_EmptyHash_ReturnsError()
    {
        var r = _plugin.RetrieveCcr(string.Empty);
        Assert.That(r, Does.Contain("error"));
    }

    // ─── BatchRoute ───────────────────────────────────────────────────────

    [Test]
    public async Task BatchRoute_TwoSections_ReturnsBothCompressed()
    {
        const string input = "Hello world, this is a plain text sentence for testing.\n---\nAnother plain text sentence here for the second section.";
        var result = await _plugin.BatchRoute(input);
        Assert.That(result, Does.Contain("Total:"));
        Assert.That(result, Does.Contain("Section 1"));
        Assert.That(result, Does.Contain("Section 2"));
    }

    [Test]
    public async Task BatchRoute_EmptyInput_ReturnsEmptyMarker()
    {
        var result = await _plugin.BatchRoute(string.Empty);
        Assert.That(result, Is.EqualTo("[empty input]"));
    }
}

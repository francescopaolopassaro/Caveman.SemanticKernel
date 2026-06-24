using caveman.core.SemanticKernel.Plugin;

namespace caveman.tests;

[TestFixture]
public class CavemanConversationPluginTests
{
    private const string Discourse =
        "Nel piccolo villaggio di Valchiara, situato ai piedi di una montagna perennemente innevata, viveva un uomo di nome Elia. " +
        "A differenza degli altri abitanti, Elia non faceva il boscaiolo o il pastore, ma aveva un mestiere del tutto particolare: era un collezionista di ombre. " +
        "Fin da quando era ragazzo, Elia aveva scoperto di possedere un dono straordinario e lo coltivava ogni giorno con grande pazienza. " +
        "Grazie a una piccola lanterna di ottone e a un pizzico di polvere di stelle, riusciva a staccare l'ombra dalle persone e dagli oggetti. " +
        "Non rubava le ombre per fare del male, ma per preservare i ricordi felici di tutti gli abitanti del paese. " +
        "Gli abitanti del villaggio, tuttavia, non capivano questa sua passione e lo evitavano, considerandolo uno stravagante stregone solitario.";

    private CavemanConversationPlugin _plugin;

    [SetUp]
    public void Setup() => _plugin = new CavemanConversationPlugin();

    [Test]
    public void EstimateTokens_ReturnsPositiveCount()
    {
        var tokens = _plugin.EstimateTokens(Discourse, "gpt-4");
        Assert.That(int.Parse(tokens), Is.GreaterThan(0));
    }

    [Test]
    public void ExtractMemory_ReturnsNonEmptyNote()
    {
        var memory = _plugin.ExtractMemory(Discourse, 3, 6);
        Assert.That(memory, Is.Not.Empty);
        Assert.That(memory, Does.Contain("Elia"));
    }

    [Test]
    public void FocusConversation_KeepsRelevantBlock()
    {
        var focused = _plugin.FocusConversation(
            "Il cielo è azzurro.\n\nLa capitale d'Italia è Roma.", "Qual è la capitale?", 1);
        Assert.That(focused, Does.Contain("Roma"));
    }

    [Test]
    public void SummarizeConversation_CondensesLongDiscourse()
    {
        var summary = _plugin.SummarizeConversation(Discourse, parseRoles: false);
        Assert.That(summary, Is.Not.Empty);
        Assert.That(summary.Length, Is.LessThan(Discourse.Length));
    }

    [Test]
    public void FitToBudget_HonorsBudget()
    {
        var json = "[ { \"role\": \"user\", \"content\": " +
                   System.Text.Json.JsonSerializer.Serialize(Discourse + " " + Discourse) +
                   " }, { \"role\": \"assistant\", \"content\": \"Ok.\" } ]";

        var fitted = _plugin.FitToBudget(json, maxTokens: 60, model: "gpt-4", keepLastTurns: 1);

        Assert.That(fitted, Is.Not.Empty);
        Assert.That(new caveman.core.services.ModelTokenizer().CountTokens(fitted), Is.LessThanOrEqualTo(60));
    }

    // ── CavemanSharedContext ─────────────────────────────────────────────────

    [Test]
    public void ShareContext_ReturnsTokenSavingsSummary()
    {
        var result = _plugin.ShareContext("test-key", Discourse);
        Assert.That(result, Does.Contain("test-key"));
        Assert.That(result, Does.Contain("tokens"));
    }

    [Test]
    public void GetSharedContext_ReturnsCompressedContent()
    {
        _plugin.ShareContext("ctx-read", Discourse);
        var retrieved = _plugin.GetSharedContext("ctx-read");
        Assert.That(retrieved, Is.Not.Null.And.Not.Empty);
        Assert.That(retrieved, Does.Not.Contain("[error:"));
    }

    [Test]
    public void GetSharedContext_UnknownKey_ReturnsError()
    {
        var result = _plugin.GetSharedContext("nonexistent-key-xyz");
        Assert.That(result, Does.Contain("[error:"));
    }

    [Test]
    public void SharedContextStats_AfterPut_ReportsEntries()
    {
        _plugin.ShareContext("stats-key", Discourse);
        var stats = _plugin.SharedContextStats();
        Assert.That(stats, Does.Contain("entries").Or.Contain("entry"));
    }

    // ── CavemanMessageDeduplicator ───────────────────────────────────────────

    [Test]
    public void DedupMessages_NoDuplicates_ReturnsNoneFound()
    {
        var messages = "Hello world, this is the first message.\n---\nThis is an entirely different second message.";
        var result = _plugin.DedupMessages(messages);
        Assert.That(result, Does.Contain("no duplicates"));
    }

    [Test]
    public void DedupMessages_WithDuplicates_ReportsDuplicate()
    {
        var longMsg = new string('A', 60); // > MinContentLength (50)
        var messages = string.Join("\n---\n", longMsg, "different msg 1", "different msg 2", "different msg 3", longMsg);
        var result = _plugin.DedupMessages(messages);
        Assert.That(result, Does.Contain("duplicate"));
    }

    [Test]
    public void CleanDuplicateMessages_WithDuplicates_ReplacesWithPlaceholder()
    {
        var longMsg = new string('B', 60);
        var messages = string.Join("\n---\n", longMsg, "msg2 text here different", "msg3 text here different", "msg4 text here different", longMsg);
        var cleaned = _plugin.CleanDuplicateMessages(messages);
        Assert.That(cleaned, Does.Contain("[duplicate of message #"));
    }
}

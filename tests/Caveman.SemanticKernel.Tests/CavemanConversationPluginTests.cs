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
}

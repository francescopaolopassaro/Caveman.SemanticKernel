// -----------------------------------------------------------------------------
// <copyright file="CavemanServicesPluginTests.cs" company="Digitalsolutions.it">
//   Caveman.SemanticKernel — Semantic Kernel plugins for Caveman.
//   Copyright (c) 2026 Passaro Francesco Paolo — Digitalsolutions.it.
//   Licensed under the Caveman License (MIT + mandatory attribution).
// </copyright>
// <summary>Unit tests for CavemanServicesPlugin (v1.4.4: compress_sql).</summary>
// -----------------------------------------------------------------------------
using caveman.core.SemanticKernel.Plugin;

namespace caveman.tests;

[TestFixture]
public class CavemanServicesPluginTests
{
    private CavemanServicesPlugin _plugin;

    [SetUp]
    public void Setup() => _plugin = new CavemanServicesPlugin();

    // ─── CompressSql ──────────────────────────────────────────────────────

    [Test]
    public void CompressSql_FormattedSelect_CollapsesWhitespace()
    {
        const string sql = "SELECT id,\n  name\nFROM users\nWHERE status = 'active';";
        var r = _plugin.CompressSql(sql);
        Assert.That(r, Is.Not.EqualTo(sql));
        Assert.That(r, Does.Contain("SELECT id, name"));
        Assert.That(r, Does.Contain("'active'"));
    }

    [Test]
    public void CompressSql_StripComments_RemovesComments()
    {
        const string sql = "-- header comment\nSELECT * FROM t /* inline */ WHERE a = 1;";
        var r = _plugin.CompressSql(sql, stripComments: true);
        Assert.That(r, Does.Not.Contain("header comment"));
        Assert.That(r, Does.Contain("SELECT"));
    }

    [Test]
    public void CompressSql_EmptyInput_ReturnsEmpty()
    {
        Assert.That(_plugin.CompressSql(string.Empty), Is.EqualTo(string.Empty));
    }
}

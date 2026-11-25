#nullable enable
using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.ContentPack;
using Content.IntegrationTests.Utility;
using Content.Shared.Guidebook;
using Content.IntegrationTests.Fixtures.Attributes;

namespace Content.IntegrationTests.Tests.Guidebook;

[TestOf(typeof(GuidebookSystem))]
[TestOf(typeof(GuideEntryPrototype))]
[TestOf(typeof(DocumentParsingManager))]
public sealed class GuideEntryPrototypeTests : GameTest
{
    private static string[] _guideEntries = GameDataScrounger.PrototypesOfKind<GuideEntryPrototype>();

    [SidedDependency(Side.Client)] private IResourceManager _cResMan = null!;
    [SidedDependency(Side.Client)] private DocumentParsingManager _cParser = null!;

    [Test]
    [TestCaseSource(nameof(_guideEntries))]
    [Description("Ensures a given guidebook entry is valid, checking the document/etc.")]
    [RunOnSide(Side.Client)]
    public async Task Validate(string protoKey)
    {
        var proto = CProtoMan.Index<GuideEntryPrototype>(protoKey);
        using var reader = _cResMan.ContentFileReadText(proto.Text);
        var text = reader.ReadToEnd();

        Assert.That(_cParser.TryAddMarkup(new Document(), text), $"Failed to parse the guide entry's document.");
    }
}

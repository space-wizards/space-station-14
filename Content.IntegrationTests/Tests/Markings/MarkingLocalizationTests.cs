using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Markings;

[TestFixture]
public sealed class MarkingsLocalizationTests : GameTest
{
    private static string[] _markings = GameDataScrounger.PrototypesOfKind<MarkingPrototype>();
    
    [Test]
    [TestOf(typeof(MarkingPrototype))]
    [TestCaseSource(nameof(_markings))]
    [Description("Tests that a given marking has defined localizations for itself and all layers (if colorable).")]
    public async Task MarkingHasLocalization(string marking)
    {
        var pair = Pair;
        var server = pair.Server;

        var protoManager = server.ProtoMan;
        var locManager = server.ResolveDependency<ILocalizationManager>();
        
        var proto = protoManager.Index<MarkingPrototype>(marking);
            
        if (proto.GroupWhitelist is null || proto.GroupWhitelist.Count == 0)
            return;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(locManager.HasString($"marking-{proto.ID}"),
                $"Marking {proto.ID} is missing localization for: marking-{proto.ID}");

            if (proto.ForcedColoring) 
                return;
            
            foreach (var sprite in proto.Sprites)
            {
                var locStr = "";
                switch (sprite)
                {
                    case SpriteSpecifier.Rsi rsi:
                        locStr = $"marking-{proto.ID}-{rsi.RsiState}";
                        break;
                    case SpriteSpecifier.Texture texture:
                        locStr = $"marking-{proto.ID}-{texture.TexturePath.Filename}";
                        break;
                    default:
                        Assert.Fail();
                        break;
                }
                    
                Assert.That(locManager.HasString(locStr),
                    $"Marking {proto.ID} is missing localization for: {locStr}");
            }
        }
    }
}
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Markings;

public sealed class MarkingLocalizationTests : GameTest
{
    [SidedDependency(Side.Server)] private readonly ILocalizationManager _sLocManager = default!;

    private static readonly string[] Markings = GameDataScrounger.PrototypesOfKind<MarkingPrototype>();

    [Test]
    [TestOf(typeof(MarkingPrototype))]
    [Description("Tests that a given marking has defined localizations for itself and all layers (if colorable).")]
    public async Task MarkingHasLocalization()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var marking in Markings)
            {
                var proto = SProtoMan.Index<MarkingPrototype>(marking);

                if (proto.GroupWhitelist is null || proto.GroupWhitelist.Count == 0)
                    continue;

                Assert.That(_sLocManager.HasString($"marking-{proto.ID}"),
                        $"Marking {proto.ID} is missing localization for: marking-{proto.ID}");

                if (proto.ForcedColoring)
                    continue;

                foreach (var sprite in proto.Sprites)
                {
                    var locStr = sprite switch
                    {
                        SpriteSpecifier.Rsi rsi => $"marking-{proto.ID}-{rsi.RsiState}",
                        SpriteSpecifier.Texture texture => $"marking-{proto.ID}-{texture.TexturePath.Filename}",
                        _ => ""
                    };

                    Assert.That(locStr != "", $"Unhandled SpriteSpecifier type {sprite} in marking {proto.ID}");

                    Assert.That(_sLocManager.HasString(locStr),
                            $"Marking {proto.ID} is missing localization for: {locStr}");
                }
            }
        }
    }
}

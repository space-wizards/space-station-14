using System.Collections.Generic;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests.Localization;

public sealed class MarkingPrototypeLocalizationTest
{
    // These categories do not show localized state names anyway, so localizing the state here is redundant.
    // NOTE: Should this be localized anyway??
    private readonly HashSet<MarkingCategories> _excludedStateCategories = [
        MarkingCategories.Hair,
        MarkingCategories.FacialHair
    ];

    [Test]
    public async Task AllMarkingsLocalized()
    {
        await using var pair = await PoolManager.GetServerClient();

        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var locMan = server.ResolveDependency<ILocalizationManager>();

        Assert.Multiple(() =>
        {
            foreach (var proto in protoMan.EnumeratePrototypes<MarkingPrototype>())
            {
                if (proto.SpeciesRestrictions.Count == 0) // Won't show up in the marking picker anyway!
                    continue;

                var nameId = proto.GetNameLocale();
                Assert.That(locMan.HasString(nameId),
                    $"Marking {proto.ID} lacks a localized name string matching {nameId}!");

                // Neither markings with forced coloring nor hair/facial hair will show their state names anyway.
                if (proto.ForcedColoring || _excludedStateCategories.Contains(proto.MarkingCategory))
                    continue;

                foreach (var state in proto.Sprites)
                {
                    var stateId = MarkingManager.GetMarkingStateId(proto, state);
                    Assert.That(locMan.HasString(stateId),
                        $"Marking {proto.ID} lacks a localized state string matching {stateId}!");
                }
            }
        });

        await pair.CleanReturnAsync();
    }
}

using System.Collections.Generic;
using System.Linq;
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
                if (proto.SpeciesRestrictions?.Count == 0) // won't show up in the marking picker anyway
                    continue;

                var missingLocales = new List<string>();

                var nameId = proto.GetNameLocale();
                if (!locMan.HasString(nameId))
                    missingLocales.Add(nameId);

                // markings with forced coloring, hair, and facial hair will not show their layer states anyway
                var statesExcluded = proto.ForcedColoring || _excludedStateCategories.Contains(proto.MarkingCategory);
                if (!statesExcluded)
                    GetMissingStateLocales(proto, ref missingLocales, locMan);

                Assert.That(missingLocales, Is.Empty,
                    $"Marking {proto.ID} is missing the following localization strings: {string.Join(", ", missingLocales)}");
            }
        });

        await pair.CleanReturnAsync();
    }

    private void GetMissingStateLocales(MarkingPrototype marking, ref List<string> missingLocales, ILocalizationManager locMan)
    {
        foreach (var state in marking.Sprites)
        {
            var stateId = MarkingManager.GetMarkingStateId(marking, state);
            if (!locMan.HasString(stateId))
                missingLocales.Add(stateId);
        }
    }
}

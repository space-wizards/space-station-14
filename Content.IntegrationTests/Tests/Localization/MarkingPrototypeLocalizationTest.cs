using System.Collections.Generic;
using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests.Localization;

public sealed class MarkingPrototypeLocalizationTest
{
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
                // markings with empty group whitelists won't show up in the marking picker anyway
                if (proto.GroupWhitelist?.Count == 0)
                    continue;

                var missingLocales = new List<string>();

                var nameId = proto.GetNameLocale();
                if (!locMan.HasString(nameId))
                    missingLocales.Add(nameId);

                // markings with forced coloring will not show their layer states anyway
                var statesExcluded = proto.ForcedColoring;
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

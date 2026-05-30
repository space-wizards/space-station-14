using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests.Localization;

[TestFixture]
public sealed class MarkingPrototypeLocalizationTest : GameTest
{
    private static readonly string[] Markings = GameDataScrounger.PrototypesOfKind<MarkingPrototype>();

    [Test]
    [TestCaseSource(nameof(Markings))]
    [TestOf(typeof(MarkingPrototype))]
    [Description("Ensures that all markings would be properly localized in the marking editor UI.")]
    public async Task AllMarkingsLocalized(string marking)
    {
        var server = Pair.Server;
        var protoMan = server.ProtoMan;
        var locMan = server.ResolveDependency<ILocalizationManager>();
        var proto = protoMan.Index<MarkingPrototype>(marking);
        var statesExcluded = proto.ForcedColoring; // markings with forced coloring will not show their layer states anyway

        // markings with empty group whitelists won't show up in the marking picker anyway
        if (proto.GroupWhitelist?.Count == 0)
            return;

        await server.WaitAssertion(() =>
        {
            var missingLocales = new List<string>();

            // marking name locales
            var nameId = proto.GetNameLocale();
            if (!locMan.HasString(nameId))
                missingLocales.Add(nameId);

            // marking layer locales
            if (!statesExcluded)
                GetMissingStateLocales(proto, ref missingLocales, locMan);

            Assert.That(missingLocales, Is.Empty,
                $"Marking {proto.ID} is missing the following localization strings: {string.Join(", ", missingLocales)}");
        });
    }

    private static void GetMissingStateLocales(MarkingPrototype marking,
        ref List<string> missingLocales,
        ILocalizationManager locMan)
    {
        foreach (var state in marking.Sprites)
        {
            var stateId = MarkingManager.GetMarkingStateId(marking, state);
            if (!locMan.HasString(stateId))
                missingLocales.Add(stateId);
        }
    }
}

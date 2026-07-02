using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Lathe;
using Content.Shared.Prototypes;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Lathe;

[TestFixture]
public sealed class VoiceCommandRecipeAmbiguityTest : GameTest
{
    [Test]
    public void LocalizedQuantityWordsMatchTest()
    {
        var locMan = Pair.Server.ResolveDependency<ILocalizationManager>();
        var digits = VoiceCommandMatcher.BuildSpokenDigits(id => locMan.GetString(id));
        var five = digits.FirstOrDefault(kvp => kvp.Value == 5).Key;
        Assert.That(five, Is.Not.Empty, "Localized quantity words must include a word for 5.");

        var triggers = new Dictionary<string, string>
        {
            ["popcorn"] = "FoodPopcorn",
        };
        var comp = new VoiceCommandsComponent
        {
            Triggers = triggers,
            Candidates = VoiceCommandMatcher.BuildVoiceCommandCandidates(triggers),
        };

        Assert.Multiple(() =>
        {
            Assert.That(VoiceCommandMatcher.TryMatchVoiceCommand(comp, $"{five} popcorn", digits, out var tag, out var quantity), Is.True);
            Assert.That(tag, Is.EqualTo("FoodPopcorn"));
            Assert.That(quantity, Is.EqualTo(5));

            Assert.That(VoiceCommandMatcher.TryMatchVoiceCommand(comp, $"{five}, popcorn", digits, out tag, out quantity), Is.True);
            Assert.That(tag, Is.EqualTo("FoodPopcorn"));
            Assert.That(quantity, Is.EqualTo(5));
        });
    }

    // Every recipe a voice lathe registers should resolve back to itself.
    [Test]
    public async Task RecipeNamesSelfResolveTest()
    {
        var locMan = Server.ResolveDependency<ILocalizationManager>();
        var latheSystem = Server.System<SharedLatheSystem>();
        var digits = VoiceCommandMatcher.BuildSpokenDigits(id => locMan.GetString(id));

        var map = await Pair.CreateTestMap();

        // HasComponent resolves the component factory via IoC, so enumerate on the server thread.
        var latheIds = new List<string>();
        await Server.WaitPost(() =>
        {
            latheIds = SProtoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !Pair.IsTestPrototype(p))
                .Where(p => p.HasComponent<LatheComponent>() && p.HasComponent<VoiceCommandsComponent>())
                .Select(p => p.ID)
                .ToList();
        });

        Assert.That(latheIds, Is.Not.Empty, "No voice-command lathe prototypes found to test.");

        // Spawn each lathe via the tracked helper; mutation runs on the server thread and cleans up after.
        var lathes = new List<(string Id, EntityUid Uid)>();
        foreach (var id in latheIds)
            lathes.Add((id, await SpawnAtPosition(id, map.GridCoords)));

        var failures = new List<string>();

        // Reads only; recipe-name resolution needs the server thread's IoC context.
        await Server.WaitAssertion(() =>
        {
            foreach (var (id, uid) in lathes)
            {
                // Candidates come from the provider path on MapInit.
                var comp = SComp<VoiceCommandsComponent>(uid);
                if (comp.Candidates.Count == 0)
                    failures.Add($"{id}: voice lathe contributes no recipe triggers");
                else
                    CheckLathe(id, latheSystem, comp, digits, failures);
            }
        });

        Assert.That(failures, Is.Empty, $"{failures.Count} voice recipe command failure(s):\n{string.Join('\n', failures)}");
    }

    private static void CheckLathe(string latheId, SharedLatheSystem latheSystem, VoiceCommandsComponent comp, IReadOnlyDictionary<string, int> digits, List<string> failures)
    {
        foreach (var candidate in comp.Candidates)
        {
            // The registered phrase is the recipe's display name.
            var phrase = latheSystem.GetRecipeName(candidate.Tag).Trim();
            if (!VoiceCommandMatcher.TryMatchVoiceCommand(comp, phrase, digits, out var got, out _))
                failures.Add($"{latheId}: \"{phrase}\" ({candidate.Tag}) matched nothing");
            else if (got != candidate.Tag)
                failures.Add($"{latheId}: \"{phrase}\" ({candidate.Tag}) cross-matched {got} (\"{latheSystem.GetRecipeName(got).Trim()}\")");
        }
    }
}

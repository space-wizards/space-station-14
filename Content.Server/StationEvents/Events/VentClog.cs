using System.Linq;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Chemistry.ReactionEffects;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class VentClog : StationEventSystem
{
    public override string Prototype => "VentClog";

    public readonly IReadOnlyList<string> SafeishVentChemicals = new[]
    {
        "Water", "Iron", "Oxygen", "Tritium", "Plasma", "SulfuricAcid", "Blood", "SpaceDrugs", "SpaceCleaner", "Flour",
        "Nutriment", "Sugar", "SpaceLube", "Ethanol", "Mercury", "Ephedrine", "WeldingFuel", "VentCrud"
    };

    public override void Started()
    {
        base.Started();

        // TODO: "safe random" for chems. Right now this includes admin chemicals.
        var allReagents = PrototypeManager.EnumeratePrototypes<ReagentPrototype>()
            .Where(x => !x.Abstract)
            .Select(x => x.ID).ToList();

        // This is gross, but not much can be done until event refactor, which needs Dynamic.
        var sound = new SoundPathSpecifier("/Audio/Effects/extinguish.ogg");

        foreach (var (_, transform) in EntityManager.EntityQuery<GasVentPumpComponent, TransformComponent>())
        {
            var solution = new Solution();

            if (!RobustRandom.Prob(0.33f))
                continue;

            if (RobustRandom.Prob(0.05f))
            {
                solution.AddReagent(RobustRandom.Pick(allReagents), 100);
            }
            else
            {
                solution.AddReagent(RobustRandom.Pick(SafeishVentChemicals), 100);
            }

            FoamAreaReactionEffect.SpawnFoam("Foam", transform.Coordinates, solution, RobustRandom.Next(2, 6), 20, 1,
                1, sound, EntityManager);
        }
    }

}

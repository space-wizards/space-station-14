using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Chemistry.ReactionEffects;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class VentClog : StationEventSystem
{
    public override string Prototype => "VentClog";

    public readonly IReadOnlyList<string> SafeishVentChemicals = new[]
    {
        "Water", "Blood", "Slime", "Acetone", "SpaceDrugs", "SpaceCleaner", "Nutriment", "Sugar", "SpaceLube", "Ethanol", "Ephedrine", "WeldingFuel", "VentCrud", "Ale", "Beer"
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
        var mod = (float) Math.Sqrt(GetSeverityModifier());

        foreach (var (_, transform) in EntityManager.EntityQuery<GasVentPumpComponent, TransformComponent>())
        {
            var solution = new Solution();

            if (!RobustRandom.Prob(Math.Min(0.33f * mod, 1.0f)))
                continue;

            if (RobustRandom.Prob(Math.Min(0.05f * mod, 1.0f)))
            {
                solution.AddReagent(RobustRandom.Pick(allReagents), 100);
            }
            else
            {
                solution.AddReagent(RobustRandom.Pick(SafeishVentChemicals), 100);
            }

            FoamAreaReactionEffect.SpawnFoam("Foam", transform.Coordinates, solution, (int) (RobustRandom.Next(2, 6) * mod), 20, 1,
                1, sound, EntityManager);
        }
    }

}

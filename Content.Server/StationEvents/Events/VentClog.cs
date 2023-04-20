using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Station.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class VentClog : StationEventSystem
{
    public override string Prototype => "VentClog";

    public readonly IReadOnlyList<string> SafeishVentChemicals = new[]
    {
        "Water", "Blood", "Slime", "SpaceDrugs", "SpaceCleaner", "Nutriment", "Sugar", "SpaceLube", "Ephedrine", "Ale", "Beer"
    };

    public override void Started()
    {
        base.Started();

        if (StationSystem.Stations.Count == 0)
            return;
        var chosenStation = RobustRandom.Pick(StationSystem.Stations.ToList());

        // TODO: "safe random" for chems. Right now this includes admin chemicals.
        var allReagents = PrototypeManager.EnumeratePrototypes<ReagentPrototype>()
            .Where(x => !x.Abstract)
            .Select(x => x.ID).ToList();

        // This is gross, but not much can be done until event refactor, which needs Dynamic.
        var sound = new SoundPathSpecifier("/Audio/Effects/extinguish.ogg");
        var mod = (float) Math.Sqrt(GetSeverityModifier());

        foreach (var (_, transform) in EntityManager.EntityQuery<GasVentPumpComponent, TransformComponent>())
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station != chosenStation)
            {
                continue;
            }

            var solution = new Solution();

            if (!RobustRandom.Prob(Math.Min(0.33f * mod, 1.0f)))
                continue;

            if (RobustRandom.Prob(Math.Min(0.05f * mod, 1.0f)))
            {
                solution.AddReagent(RobustRandom.Pick(allReagents), 200);
            }
            else
            {
                solution.AddReagent(RobustRandom.Pick(SafeishVentChemicals), 200);
            }

            var foamEnt = Spawn("Foam", transform.Coordinates);
            var smoke = EnsureComp<SmokeComponent>(foamEnt);
            smoke.SpreadAmount = 20;
            EntityManager.System<SmokeSystem>().Start(foamEnt, smoke, solution, 20f);
            EntityManager.System<AudioSystem>().PlayPvs(sound, transform.Coordinates);
        }
    }

}

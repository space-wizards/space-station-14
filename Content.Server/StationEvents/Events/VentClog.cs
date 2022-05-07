using System.Linq;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Chemistry.ReactionEffects;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class VentClog : StationEvent
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override string Name => "VentClog";

    public override string? StartAnnouncement =>
        Loc.GetString("station-event-vent-clog-start-announcement");

    public override int EarliestStart => 15;

    public override int MinimumPlayers => 15;

    public override float Weight => WeightLow;

    public override int? MaxOccurrences => 2;

    // Give players time to reach cover.
    protected override float StartAfter => 50f;

    protected override float EndAfter => 51.0f; // This can, surprisingly, cause the event to end before it starts.

    public readonly IReadOnlyList<string> SafeishVentChemicals = new[]
    {
        "Water", "Iron", "Oxygen", "Tritium", "Plasma", "SulfuricAcid", "Blood", "SpaceDrugs", "SpaceCleaner", "Flour",
        "Nutriment", "Sugar", "SpaceLube", "Ethanol", "Mercury", "Ephedrine", "WeldingFuel", "VentCrud"
    };

    public override void Startup()
    {
        base.Startup();

        // TODO: "safe random" for chems. Right now this includes admin chemicals.
        var allReagents = _prototypeManager.EnumeratePrototypes<ReagentPrototype>()
            .Where(x => !x.Abstract)
            .Select(x => x.ID).ToList();

        // This is gross, but not much can be done until event refactor, which needs Dynamic.
        var sound = new SoundPathSpecifier("/Audio/Effects/extinguish.ogg");

        foreach (var (_, transform) in _entityManager.EntityQuery<GasVentPumpComponent, TransformComponent>())
        {
            var solution = new Solution();

            if (_random.Prob(0.05f))
            {
                solution.AddReagent(_random.Pick(allReagents), 100);
            }
            else
            {
                solution.AddReagent(_random.Pick(SafeishVentChemicals), 100);
            }

            FoamAreaReactionEffect.SpawnFoam("Foam", transform.Coordinates, solution, _random.Next(2, 6), 20, 1,
                1, sound, _entityManager);
        }
    }

}

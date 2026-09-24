using Content.Shared.Antag;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Abilities;

public abstract partial class CosmicFragmentationSystem : EntitySystem
{
    [Dependency] protected AntagSelectionSystem Antag = default!;

    [Dependency] private CosmicCultSystem _cult = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    protected EntProtoId IndicatorEffect = "EffectCosmicBigWindup";

    private void UnEmpower(Entity<CosmicCultistComponent?> ent)
    {
        var ev = new CosmicCultistEmpowerChangedEvent(ent, false);
        RaiseLocalEvent(ent, ref ev);

        if (ent.Comp != null)
            ent.Comp.WasEmpowered = true;
    }

    [SubscribeLocalEvent]
    private void OnCosmicFragmentation(Entity<CosmicActionFragmentationComponent> ent, ref EventCosmicFragmentation args)
    {
        if (!_cult.CultActionQuery.HasComp(ent))
            return;

        if (args.Handled || _mobState.IsIncapacitated(args.Target))
            return;

        if (HasComp<BorgChassisComponent>(args.Target) && !_mind.TryGetMind(args.Target, out _, out _))
            return;

        var evt = new MalignFragmentationEvent(args.Target, false);
        RaiseLocalEvent(args.Target, ref evt);

        if (!evt.Cancelled)
            UnEmpower(args.Performer);

        args.Handled = !evt.Cancelled;
    }

    [SubscribeLocalEvent]
    private void OnFragmentBorg(Entity<BorgChassisComponent> ent, ref MalignFragmentationEvent args)
    {
        var chantry = Spawn("CosmicBorgChantry", Transform(ent).Coordinates);
        EnsureComp<CosmicChantryComponent>(chantry, out var chantryComponent);
        Spawn(IndicatorEffect, Transform(chantry).Coordinates);

        if (_container.TryGetContainer(chantry, SharedEntityStorageSystem.ContainerName, out var container))
            _container.Insert(args.Target, container);

        chantryComponent.InternalVictim = args.Target;
        var mins = chantryComponent.EventTime.Minutes;
        var secs = chantryComponent.EventTime.Seconds;
        Antag.SendBriefing(args.Target, Loc.GetString("cosmiccult-silicon-chantry-briefing", ("minutesandseconds", $"{mins} minutes and {secs} seconds")), Color.FromHex("#4cabb3"), null);
    }
}

[ByRefEvent]
public record struct MalignFragmentationEvent(EntityUid Target, bool Cancelled);

using Content.Server.Antag;
using Content.Server.Silicons.Laws;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio.Components;
using Content.Shared.Radio;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class CosmicFragmentationSystem : EntitySystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    private ProtoId<RadioChannelPrototype> _cultRadio = "CosmicRadio";

    private void UnEmpower(Entity<CosmicCultistComponent> ent)
    {
        var comp = ent.Comp;
        comp.CosmicEmpowered = false;
        comp.CosmicSiphonQuantity = CosmicCultistComponent.DefaultCosmicSiphonQuantity;
        comp.CosmicGlareRange = CosmicCultistComponent.DefaultCosmicGlareRange;
        comp.CosmicGlareDuration = CosmicCultistComponent.DefaultCosmicGlareDuration;
        comp.CosmicGlareStun = CosmicCultistComponent.DefaultCosmicGlareStun;
        comp.CosmicImpositionDuration = CosmicCultistComponent.DefaultCosmicImpositionDuration;
        comp.CosmicShuntDuration = CosmicCultistComponent.DefaultCosmicShuntDuration;
        comp.CosmicShuntDelay = CosmicCultistComponent.DefaultCosmicShuntDelay;
        comp.CosmicShiftWindup = CosmicCultistComponent.DefaultCosmicShiftWindup;
    }

    [SubscribeLocalEvent]
    private void OnCosmicFragmentation(Entity<CosmicCultistComponent> ent, ref EventCosmicFragmentation args)
    {
        if (args.Handled || _mobState.IsIncapacitated(args.Target))
            return;

        if (HasComp<BorgChassisComponent>(args.Target) && !_mind.TryGetMind(ent, out _, out _))
            return; // Don't waste charges on borgs that ain't here.

        args.Handled = true;
        var evt = new MalignFragmentationEvent(ent, args.Target);
        RaiseLocalEvent(args.Target, ref evt);
    }

    [SubscribeLocalEvent]
    private void OnFragmentBorg(Entity<BorgChassisComponent> ent, ref MalignFragmentationEvent args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mind))
            return;

        var wisp = Spawn("CosmicChantryWisp", Transform(ent).Coordinates);
        var chantry = Spawn("CosmicBorgChantry", Transform(ent).Coordinates);
        EnsureComp<CosmicChantryComponent>(chantry, out var chantryComponent);
        chantryComponent.InternalVictim = wisp;
        chantryComponent.VictimBody = ent;
        _mind.TransferTo(mindId, wisp, mind: mind);

        var mins = chantryComponent.EventTime.Minutes;
        var secs = chantryComponent.EventTime.Seconds;
        _antag.SendBriefing(wisp, Loc.GetString("cosmiccult-silicon-chantry-briefing", ("minutesandseconds", $"{mins} minutes and {secs} seconds")), Color.FromHex("#4cabb3"), null);
    }

    [SubscribeLocalEvent]
    private void OnFragmentAi(Entity<SiliconLawUpdaterComponent> ent, ref MalignFragmentationEvent args)
    {
        var lawboard = Spawn("CosmicCultLawBoard", Transform(args.Target).Coordinates);
        _container.TryGetContainer(args.Target, "circuit_holder", out var container);
        if (container == null)
            return;
        _container.EmptyContainer(container, true);
        _container.Insert(lawboard, container, Transform(args.Target), true);
    }

    [SubscribeLocalEvent]
    private void OnLawInserted(ref AILawUpdatedEvent args)
    {
        if (!TryComp<IntrinsicRadioTransmitterComponent>(args.Target, out var radio) || !TryComp<ActiveRadioComponent>(args.Target, out var transmitter))
            return;
        if (args.Lawset.Id == "CosmicCultLaws")
        {
            radio.Channels.Add(_cultRadio);
            transmitter.Channels.Add(_cultRadio);
            _antag.SendBriefing(args.Target, Loc.GetString("cosmiccult-silicon-subverted-briefing"), Color.FromHex("#4cabb3"), null);
        }
        else
        {
            radio.Channels.Remove(_cultRadio);
            transmitter.Channels.Remove(_cultRadio);
        }
    }
}

[ByRefEvent]
public record struct MalignFragmentationEvent(Entity<CosmicCultistComponent> User, EntityUid Target);

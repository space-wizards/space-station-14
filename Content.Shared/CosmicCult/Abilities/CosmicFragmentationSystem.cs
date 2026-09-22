using Content.Shared.Antag;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radio.Components;
using Content.Shared.Radio;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Abilities;

public sealed partial class CosmicFragmentationSystem : EntitySystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private CosmicCultSystem _cult = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedStationAiSystem _ai = default!;
    [Dependency] private SharedSiliconLawSystem _laws = default!;

    private EntProtoId _indicatorEffect = "EffectCosmicBigWindup";
    private ProtoId<RadioChannelPrototype> _cosmicRadio = "CosmicRadio";
    private ProtoId<SiliconLawsetPrototype> _cosmicLaws = "CosmicCultLaws";
    private readonly SoundSpecifier _cosmicLawSound = new SoundPathSpecifier("/Audio/Cosmic/cosmic-ai-start.ogg");

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
            return; // Don't waste charges on borgs that ain't here.

        args.Handled = true;
        var evt = new MalignFragmentationEvent(args.Target);
        RaiseLocalEvent(args.Target, ref evt);
        UnEmpower(args.Performer);
    }

    [SubscribeLocalEvent]
    private void OnFragmentBorg(Entity<BorgChassisComponent> ent, ref MalignFragmentationEvent args)
    {
        var chantry = Spawn("CosmicBorgChantry", Transform(ent).Coordinates);
        EnsureComp<CosmicChantryComponent>(chantry, out var chantryComponent);
        Spawn(_indicatorEffect, Transform(chantry).Coordinates);

        if (_container.TryGetContainer(chantry, SharedEntityStorageSystem.ContainerName, out var container))
            _container.Insert(args.Target, container);

        chantryComponent.InternalVictim = args.Target;
        var mins = chantryComponent.EventTime.Minutes;
        var secs = chantryComponent.EventTime.Seconds;
        _antag.SendBriefing(args.Target, Loc.GetString("cosmiccult-silicon-chantry-briefing", ("minutesandseconds", $"{mins} minutes and {secs} seconds")), Color.FromHex("#4cabb3"), null);
    }

    [SubscribeLocalEvent]
    private void OnFragmentAi(Entity<StationAiCoreComponent> ent, ref MalignFragmentationEvent args)
    {
        if (!_proto.TryIndex(_cosmicLaws, out var proto))
            return;

        if (_ai.GetInsertedAI(ent) is not { } brain)
            return;

        if (!TryComp<IntrinsicRadioTransmitterComponent>(brain, out var radio) || !TryComp<ActiveRadioComponent>(brain, out var transmitter))
            return;

        var lawset = _laws.GetLawset(proto);

        radio.Channels.Add(_cosmicRadio);
        transmitter.Channels.Add(_cosmicRadio);
        _laws.SetLaws(lawset.Laws, brain, _cosmicLawSound, true);
        _antag.SendBriefing(args.Target, Loc.GetString("cosmiccult-silicon-subverted-briefing"), Color.FromHex("#4cabb3"), null);
        Spawn(_indicatorEffect, Transform(ent).Coordinates);
    }
}

[ByRefEvent]
public record struct MalignFragmentationEvent(EntityUid Target);

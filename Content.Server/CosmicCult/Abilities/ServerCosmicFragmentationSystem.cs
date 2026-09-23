using Content.Server.Silicons.Laws;
using Content.Shared.CosmicCult.Abilities;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class ServerCosmicFragmentationSystem : CosmicFragmentationSystem
{
    [Dependency] private SharedStationAiSystem _ai = default!;
    [Dependency] private SiliconLawSystem _laws = default!;

    private ProtoId<RadioChannelPrototype> _cosmicRadio = "CosmicRadio";
    private ProtoId<SiliconLawsetPrototype> _cosmicLaws = "CosmicCultLaws";
    private readonly SoundSpecifier _cosmicLawSound = new SoundPathSpecifier("/Audio/Cosmic/cosmic-ai-start.ogg");

    [SubscribeLocalEvent]
    private void OnFragmentAi(Entity<StationAiCoreComponent> ent, ref MalignFragmentationEvent args)
    {
        if (!ProtoMan.TryIndex(_cosmicLaws, out var proto))
            return;

        if (_ai.GetInsertedAI(ent) is not { } brain)
            return;

        if (!TryComp<IntrinsicRadioTransmitterComponent>(brain, out var radio) || !TryComp<ActiveRadioComponent>(brain, out var transmitter))
            return;

        var lawset = _laws.GetLawset(proto);

        radio.Channels.Add(_cosmicRadio);
        transmitter.Channels.Add(_cosmicRadio);
        _laws.SetLaws(lawset.Laws, brain, _cosmicLawSound, true);
        Antag.SendBriefing(args.Target, Loc.GetString("cosmiccult-silicon-subverted-briefing"), Color.FromHex("#4cabb3"), null);
        Spawn(IndicatorEffect, Transform(ent).Coordinates);
    }
}

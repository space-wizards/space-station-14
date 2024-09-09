using Content.Shared.Doors.Components;
using Robust.Shared.Serialization;
using Content.Shared.Electrocution;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Robust.Shared.Audio;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    private static readonly SoundPathSpecifier AirlockOverchargeDisabled = new("/Audio/Machines/airlock_overcharge_on.ogg");
    private static readonly SoundPathSpecifier AirlockOverchargeEnabled = new("/Audio/Machines/airlock_overcharge_off.ogg");

    // Handles airlock radial

    private void InitializeAirlock()
    {
        SubscribeLocalEvent<DoorBoltComponent, StationAiBoltEvent>(OnAirlockBolt);
        SubscribeLocalEvent<AirlockComponent, StationAiEmergencyAccessEvent>(OnAirlockEmergencyAccess);
        SubscribeLocalEvent<ElectrifiedComponent, StationAiElectrifiedEvent>(OnElectrified);
    }

    /// <summary>
    /// Attempts to bolt door. If wire was cut (AI or for bolts) or its not powered - notifies AI and does nothing.
    /// </summary>
    private void OnAirlockBolt(EntityUid ent, DoorBoltComponent component, StationAiBoltEvent args)
    {
        if (!TryComp<StationAiWhitelistComponent>(ent, out var whiteList))
        {
            return;
        }

        if (!whiteList.Enabled || component.BoltWireCut || !component.Powered)
        {
            _popup.PopupClient(Loc.GetString("ai-device-not-responding"), args.User, PopupType.MediumCaution);
            return;
        }

        _doors.SetBoltsDown((ent, component), args.Bolted, args.User, predicted: true);
    }

    /// <summary>
    /// Attempts to bolt door. If wire was cut (AI) or its not powered - notifies AI and does nothing.
    /// </summary>
    private void OnAirlockEmergencyAccess(EntityUid ent, AirlockComponent component, StationAiEmergencyAccessEvent args)
    {
        if (!TryComp<StationAiWhitelistComponent>(ent, out var whiteList))
        {
            return;
        }

        if (!whiteList.Enabled || !component.Powered)
        {
            _popup.PopupClient(Loc.GetString("ai-device-not-responding"), args.User, PopupType.MediumCaution);
            return;
        }

        _airlocks.SetEmergencyAccess((ent, component), args.EmergencyAccess, args.User, predicted: true);
    }

    /// <summary>
    /// Attempts to bolt door. If wire was cut (AI or for one of power-wires) or its not powered - notifies AI and does nothing.
    /// </summary>
    private void OnElectrified(EntityUid ent, ElectrifiedComponent component, StationAiElectrifiedEvent args)
    {
        if (!TryComp<ElectrifiedComponent>(ent, out var electrified)
            || !TryComp<StationAiWhitelistComponent>(ent, out var whiteList))
        {
            return;
        }

        SharedApcPowerReceiverComponent? apcPowerReceiverComponent = null;
        if (
            !whiteList.Enabled
            || electrified.IsWireCut
            || (
                PowerReceiver.ResolveApc(ent, ref apcPowerReceiverComponent)
                && !apcPowerReceiverComponent.Powered
                )
        )
        {
            _popup.PopupClient(Loc.GetString("ai-device-not-responding"), args.User, PopupType.MediumCaution);
            return;
        }

        _electrify.SetElectrified((ent, component), args.Electrified);
        var soundToPlay = component.Enabled
            ? AirlockOverchargeDisabled
            : AirlockOverchargeEnabled;
        _audio.PlayEntity(soundToPlay, args.User, ent);
    }
}

/// <summary> Event for StationAI attempt at bolting/unbolting door. </summary>
[Serializable, NetSerializable]
public sealed class StationAiBoltEvent : BaseStationAiAction
{
    /// <summary> Marker, should be door bolted or unbolted. </summary>
    public bool Bolted;
}

/// <summary> Event for StationAI attempt at setting emergency access for door on/off. </summary>
[Serializable, NetSerializable]
public sealed class StationAiEmergencyAccessEvent : BaseStationAiAction
{
    /// <summary> Marker, should door have emergency access on or off. </summary>
    public bool EmergencyAccess;
}

/// <summary> Event for StationAI attempt at electrifying/de-electrifying door. </summary>
[Serializable, NetSerializable]
public sealed class StationAiElectrifiedEvent : BaseStationAiAction
{
    /// <summary> Marker, should door be electrified or no. </summary>
    public bool Electrified;
}

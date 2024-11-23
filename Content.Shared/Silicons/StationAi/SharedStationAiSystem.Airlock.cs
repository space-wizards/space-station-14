using Content.Shared.Doors.Components;
using Robust.Shared.Serialization;
using Content.Shared.Electrocution;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
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
        if (component.BoltWireCut)
        {
            ShowDeviceNotRespondingPopup(args.User);
            return;
        }

        var setResult = _doors.TrySetBoltDown((ent, component), args.Bolted, args.User, predicted: true);
        if (!setResult)
        {
            ShowDeviceNotRespondingPopup(args.User);
        }
    }

    /// <summary>
    /// Attempts to bolt door. If wire was cut (AI) or its not powered - notifies AI and does nothing.
    /// </summary>
    private void OnAirlockEmergencyAccess(EntityUid ent, AirlockComponent component, StationAiEmergencyAccessEvent args)
    {
        if (!PowerReceiver.IsPowered(ent))
        {
            ShowDeviceNotRespondingPopup(args.User);
            return;
        }

        _airlocks.SetEmergencyAccess((ent, component), args.EmergencyAccess, args.User, predicted: true);
    }

    /// <summary>
    /// Attempts to bolt door. If wire was cut (AI or for one of power-wires) or its not powered - notifies AI and does nothing.
    /// </summary>
    private void OnElectrified(EntityUid ent, ElectrifiedComponent component, StationAiElectrifiedEvent args)
    {
        if (
            component.IsWireCut
            || !PowerReceiver.IsPowered(ent)
        )
        {
            ShowDeviceNotRespondingPopup(args.User);
            return;
        }

        _electrify.SetElectrified((ent, component), args.Electrified);
        var soundToPlay = component.Enabled
            ? component.AirlockElectrifyDisabled
            : component.AirlockElectrifyEnabled;
        _audio.PlayLocal(soundToPlay, ent, args.User);
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

using Content.Shared.Doors.Components;
using Robust.Shared.Serialization;
using Content.Shared.Electrocution;
using Content.Shared.Popups;

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

    private void OnAirlockBolt(EntityUid ent, DoorBoltComponent component, StationAiBoltEvent args)
    {
        if (
            !TryComp<DoorBoltComponent>(ent, out var doorBolt)
            || !TryComp<StationAiWhitelistComponent>(ent, out var whiteList))
        {
            return;
        }

        if (!whiteList.Enabled || doorBolt.BoltWireCut)
        {
            _popup.PopupClient(Loc.GetString("ai-device-not-responding"), args.User, PopupType.MediumCaution);
            return;
        }

        _doors.SetBoltsDown((ent, component), args.Bolted, args.User, predicted: true);
    }

    private void OnAirlockEmergencyAccess(EntityUid ent, AirlockComponent component, StationAiEmergencyAccessEvent args)
    {
        if (!TryComp<StationAiWhitelistComponent>(ent, out var whiteList))
        {
            return;
        }

        if (!whiteList.Enabled)
        {
            _popup.PopupClient(Loc.GetString("ai-device-not-responding"), args.User, PopupType.MediumCaution);
            return;
        }

        _airlocks.ToggleEmergencyAccess((ent, component), args.User, predicted: true);
    }

    private void OnElectrified(EntityUid ent, ElectrifiedComponent component, StationAiElectrifiedEvent args)
    {
        if (
            !TryComp<ElectrifiedComponent>(ent, out var electrified)
            || !TryComp<StationAiWhitelistComponent>(ent, out var whiteList))
        {
            return;
        }

        if (!whiteList.Enabled || electrified.IsWireCut)
        {
            _popup.PopupClient(Loc.GetString("ai-device-not-responding"), args.User, PopupType.MediumCaution);
            return;
        }

        _electrify.ToggleElectrified((ent, component), args.User, predicted: true);
    }
}

[Serializable, NetSerializable]
public sealed class StationAiBoltEvent : BaseStationAiAction
{
    public bool Bolted;
}

[Serializable, NetSerializable]
public sealed class StationAiEmergencyAccessEvent : BaseStationAiAction
{
    public bool EmergencyAccess;
}

[Serializable, NetSerializable]
public sealed class StationAiElectrifiedEvent : BaseStationAiAction
{
    public bool Electrified;
}

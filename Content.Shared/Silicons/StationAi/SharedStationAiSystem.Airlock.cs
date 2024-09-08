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

        _electrify.ToggleElectrified((ent, component), args.User, predicted: true);
        var soundToPlay = component.Enabled
            ? AirlockOverchargeDisabled
            : AirlockOverchargeEnabled;
        _audio.PlayEntity(soundToPlay, args.User, ent);
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

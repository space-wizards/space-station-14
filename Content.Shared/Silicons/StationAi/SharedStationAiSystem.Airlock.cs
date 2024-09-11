using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Popups;
using Content.Shared.Database;

using Robust.Shared.Serialization;
using Robust.Shared.Player;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    // Handles airlock radial

    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ActorSystem _actorSystem = default!;

    private void InitializeAirlock()
    {
        SubscribeLocalEvent<DoorBoltComponent, StationAiBoltEvent>(OnAirlockBolt);
        SubscribeLocalEvent<AirlockComponent, StationAiSetEAEvent>(OnAirlockEmergencyAccess);
    }

    private void OnAirlockBolt(EntityUid ent, DoorBoltComponent component, StationAiBoltEvent args)
    {
        _doors.SetBoltsDown((ent, component), args.Bolted, args.User, predicted: true);
        if(args.Bolted)
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"Station AI {ToPrettyString(args.User)} bolted {ToPrettyString(ent)}");
        else
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"Station AI {ToPrettyString(args.User)} unbolted {ToPrettyString(ent)}");
    }

    private void OnAirlockEmergencyAccess(EntityUid ent, AirlockComponent component, StationAiSetEAEvent args)
    {
        if(component.EmergencyAccess == args.EmergencyAccess)
        {
            return;
        }
        if(TryComp<DoorComponent>(ent, out var doorComp) && !_doorSystem.HasAccess(ent, args.User, doorComp))
        {
            if(_actorSystem.TryGetSession(args.User, out var session) && session != null)
            {
                _popupSystem.PopupEntity(Loc.GetString("popup-deny-emac"), ent, session);
            }
            return;
        }

        _airlocks.ToggleEmergencyAccess(ent, component);
        if(args.EmergencyAccess)
            _adminLog.Add(LogType.Action, LogImpact.Low, $"Station AI {ToPrettyString(args.User)} set emergency access to {ToPrettyString(ent)}");
        else
            _adminLog.Add(LogType.Action, LogImpact.Low, $"Station AI {ToPrettyString(args.User)} removed emergency access to {ToPrettyString(ent)}");
    }
}

[Serializable, NetSerializable]
public sealed class StationAiBoltEvent : BaseStationAiAction
{
    public bool Bolted;
}

[Serializable, NetSerializable]
public sealed class StationAiElectrifyEvent : BaseStationAiAction
{
    public bool Electrified;
}

[Serializable, NetSerializable]
public sealed class StationAiSetEAEvent : BaseStationAiAction
{
    public bool EmergencyAccess;
}
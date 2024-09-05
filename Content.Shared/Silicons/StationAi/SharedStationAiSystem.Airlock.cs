using Content.Shared.Doors.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    // Handles airlock radial

    private void InitializeAirlock()
    {
        SubscribeLocalEvent<DoorBoltComponent, StationAiBoltEvent>(OnAirlockBolt);
        SubscribeLocalEvent<AirlockComponent, StationAiEmergencyAccessEvent>(OnAirlockEmergencyAccess);
    }

    private void OnAirlockBolt(EntityUid ent, DoorBoltComponent component, StationAiBoltEvent args)
    {
        _doors.SetBoltsDown((ent, component), args.Bolted, args.User, predicted: true);
    }
	
    private void OnAirlockEmergencyAccess(EntityUid ent, AirlockComponent component, StationAiEmergencyAccessEvent args)
    {
        _airlocks.ToggleEmergencyAccess((ent, component), args.User, predicted: true);
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

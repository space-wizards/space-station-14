using Content.Shared.Doors.Components;
using Content.Shared.Electrocution;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Utility;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    private void InitializeAirlock()
    {
        SubscribeLocalEvent<DoorBoltComponent, GetStationAiRadialEvent>(OnDoorBoltGetRadial);
        SubscribeLocalEvent<AirlockComponent, GetStationAiRadialEvent>(OnEmergencyAccessGetRadial);
        SubscribeLocalEvent<ElectrifiedComponent, GetStationAiRadialEvent>(OnDoorElectrifiedGetRadial);
    }

    private void OnDoorBoltGetRadial(Entity<DoorBoltComponent> ent, ref GetStationAiRadialEvent args)
    {
        args.Actions.Add(new StationAiRadial()
        {
            Sprite = ent.Comp.BoltsDown ?
                new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "unbolt_door") :
                new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "bolt_door"),
            Tooltip = ent.Comp.BoltsDown ? Loc.GetString("bolt-open") : Loc.GetString("bolt-close"),
            Event = new StationAiBoltEvent()
            {
                Bolted = !ent.Comp.BoltsDown,
            }
        });
    }
	
	private void OnEmergencyAccessGetRadial(Entity<AirlockComponent> ent, ref GetStationAiRadialEvent args)
    {
        args.Actions.Add(new StationAiRadial()
        {
            Sprite = ent.Comp.EmergencyAccess ?
                new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "emergency_off") :
                new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "emergency_on"),
            Tooltip = ent.Comp.EmergencyAccess ? Loc.GetString("emergency-access-off") : Loc.GetString("emergency-access-on"),
            Event = new StationAiEmergencyAccessEvent()
            {
                EmergencyAccess = !ent.Comp.EmergencyAccess,
            }
        });
    }
	
	private void OnDoorElectrifiedGetRadial(Entity<ElectrifiedComponent> ent, ref GetStationAiRadialEvent args)
    {
        args.Actions.Add(new StationAiRadial
        {
            Sprite = ent.Comp.Enabled ?
                new SpriteSpecifier.Rsi(
                    new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "unovercharge_door") :
                new SpriteSpecifier.Rsi(
                    new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "overcharge_door"),
            Tooltip = ent.Comp.Enabled ? Loc.GetString("door-overcharge-off") : Loc.GetString("door-overcharge-on"),
            Event = new StationAiElectrifiedEvent
            {
                Electrified = !ent.Comp.Enabled,
            }
        });
    }
}

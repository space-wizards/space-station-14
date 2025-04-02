using Content.Shared.Doors.Components;
using Content.Shared.Electrocution;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Utility;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    private readonly ResPath _aiActionsRsi = new ResPath("/Textures/Interface/Actions/actions_ai.rsi");

    private void InitializeAirlock()
    {
        SubscribeLocalEvent<DoorBoltComponent, GetStationAiRadialEvent>(OnDoorBoltGetRadial);
        SubscribeLocalEvent<AirlockComponent, GetStationAiRadialEvent>(OnEmergencyAccessGetRadial);
        SubscribeLocalEvent<ElectrifiedComponent, GetStationAiRadialEvent>(OnDoorElectrifiedGetRadial);
    }

    private void OnDoorBoltGetRadial(Entity<DoorBoltComponent> ent, ref GetStationAiRadialEvent args)
    {
        args.Actions.Add(
            new StationAiRadial
            {
                Sprite = ent.Comp.BoltsDown
                    ? new SpriteSpecifier.Rsi(_aiActionsRsi, "unbolt_door")
                    : new SpriteSpecifier.Rsi(_aiActionsRsi, "bolt_door"),
                Tooltip = ent.Comp.BoltsDown
                    ? Loc.GetString("bolt-open")
                    : Loc.GetString("bolt-close"),
                Event = new StationAiBoltEvent
                {
                    Bolted = !ent.Comp.BoltsDown,
                }
            }
        );
    }

    private void OnEmergencyAccessGetRadial(Entity<AirlockComponent> ent, ref GetStationAiRadialEvent args)
    {
        args.Actions.Add(
            new StationAiRadial
            {
                Sprite = ent.Comp.EmergencyAccess
                    ? new SpriteSpecifier.Rsi(_aiActionsRsi, "emergency_off")
                    : new SpriteSpecifier.Rsi(_aiActionsRsi, "emergency_on"),
                Tooltip = ent.Comp.EmergencyAccess
                    ? Loc.GetString("emergency-access-off")
                    : Loc.GetString("emergency-access-on"),
                Event = new StationAiEmergencyAccessEvent
                {
                    EmergencyAccess = !ent.Comp.EmergencyAccess,
                }
            }
        );
    }

    private void OnDoorElectrifiedGetRadial(Entity<ElectrifiedComponent> ent, ref GetStationAiRadialEvent args)
    {
        args.Actions.Add(
            new StationAiRadial
            {
                Sprite = ent.Comp.Enabled
                    ? new SpriteSpecifier.Rsi(_aiActionsRsi, "door_overcharge_off")
                    : new SpriteSpecifier.Rsi(_aiActionsRsi, "door_overcharge_on"),
                Tooltip = ent.Comp.Enabled
                    ? Loc.GetString("electrify-door-off")
                    : Loc.GetString("electrify-door-on"),
                Event = new StationAiElectrifiedEvent
                {
                    Electrified = !ent.Comp.Enabled,
                }
            }
        );
    }
}

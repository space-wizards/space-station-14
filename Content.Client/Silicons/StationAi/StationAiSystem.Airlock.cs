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
        SubscribeLocalEvent<DoorComponent, GetStationAiRadialEvent>(OnDoorGetRadial);
    }

    private void OnDoorGetRadial(Entity<DoorComponent> ent, ref GetStationAiRadialEvent args)
    {
        if (TryComp<AirlockComponent>(ent.Owner, out var airlockComp)) {
            var airlockEnt = (ent.Owner, airlockComp);

            GetRadialAirlockDoorBolt(airlockEnt, ref args);
            GetRadialAirlockEmergencyAccess(airlockEnt, ref args);
            GetRadialAirlockElectrified(airlockEnt, ref args);
        }
    }

    private void GetRadialAirlockDoorBolt(Entity<AirlockComponent> ent, ref GetStationAiRadialEvent args)
    {
        if (!TryComp<DoorBoltComponent>(ent, out var doorBolt))
            return;

        args.Actions.Add(
            new StationAiRadial
            {
                Sprite = doorBolt.BoltsDown
                    ? new SpriteSpecifier.Rsi(_aiActionsRsi, "unbolt_door")
                    : new SpriteSpecifier.Rsi(_aiActionsRsi, "bolt_door"),
                Tooltip = doorBolt.BoltsDown
                    ? Loc.GetString("bolt-open")
                    : Loc.GetString("bolt-close"),
                Event = new StationAiBoltEvent
                {
                    Bolted = !doorBolt.BoltsDown,
                }
            }
        );
    }

    private void GetRadialAirlockEmergencyAccess(Entity<AirlockComponent> ent, ref GetStationAiRadialEvent args)
    {
        var airlock = ent.Comp;

        args.Actions.Add(
            new StationAiRadial
            {
                Sprite = airlock.EmergencyAccess
                    ? new SpriteSpecifier.Rsi(_aiActionsRsi, "emergency_off")
                    : new SpriteSpecifier.Rsi(_aiActionsRsi, "emergency_on"),
                Tooltip = airlock.EmergencyAccess
                    ? Loc.GetString("emergency-access-off")
                    : Loc.GetString("emergency-access-on"),
                Event = new StationAiEmergencyAccessEvent
                {
                    EmergencyAccess = !airlock.EmergencyAccess,
                }
            }
        );
    }

    private void GetRadialAirlockElectrified(Entity<AirlockComponent> ent, ref GetStationAiRadialEvent args)
    {
        if (!TryComp<ElectrifiedComponent>(ent, out var electrified))
            return;

        args.Actions.Add(
            new StationAiRadial
            {
                Sprite = electrified.Enabled
                    ? new SpriteSpecifier.Rsi(_aiActionsRsi, "door_overcharge_off")
                    : new SpriteSpecifier.Rsi(_aiActionsRsi, "door_overcharge_on"),
                Tooltip = electrified.Enabled
                    ? Loc.GetString("electrify-door-off")
                    : Loc.GetString("electrify-door-on"),
                Event = new StationAiElectrifiedEvent
                {
                    Electrified = !electrified.Enabled,
                }
            }
        );
    }
}

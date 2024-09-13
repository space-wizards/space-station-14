using Content.Shared.Doors.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Utility;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    private void InitializeAirlock()
    {
        SubscribeLocalEvent<DoorBoltComponent, GetStationAiRadialEvent>(OnDoorBoltGetRadial);
    }

    private void OnDoorBoltGetRadial(Entity<DoorBoltComponent> ent, ref GetStationAiRadialEvent args)
    {
        args.Actions.Add(new StationAiRadial()
        {
            Sprite = ent.Comp.BoltsDown ?
                new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Structures/Doors/Airlocks/Standard/basic.rsi"), "open") :
                new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Structures/Doors/Airlocks/Standard/basic.rsi"), "closed"),
            Tooltip = ent.Comp.BoltsDown ? Loc.GetString("bolt-open") : Loc.GetString("bolt-close"),
            Event = new StationAiBoltEvent()
            {
                Bolted = !ent.Comp.BoltsDown,
            }
        });
    }
}

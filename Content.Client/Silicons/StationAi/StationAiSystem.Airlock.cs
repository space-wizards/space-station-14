using Content.Shared.Doors.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Electrocution;
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
                new ResPath("/Textures/Structures/Doors/Airlocks/Standard/security.rsi"), "open") :
                new SpriteSpecifier.Rsi(
                new ResPath("/Textures/Structures/Doors/Airlocks/Standard/security.rsi"), "closed"),
            Tooltip = ent.Comp.BoltsDown ? Loc.GetString("bolt-open") : Loc.GetString("bolt-close"),
            Event = new StationAiBoltEvent()
            {
                Bolted = !ent.Comp.BoltsDown,
            }
        });
        if(TryComp<AirlockComponent>(ent, out var AirlockComp))
        {
            args.Actions.Add(new StationAiRadial()
            {
                Sprite = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/emergency_access.png")),

                OverlaySprite = AirlockComp.EmergencyAccess ?
                    new SpriteSpecifier.Rsi(new ResPath("/Textures/Markers/cross.rsi"), "red") :
                    null,

                Tooltip = AirlockComp.EmergencyAccess ? Loc.GetString("remove-emac") : Loc.GetString("set-emac"),
                Event = new StationAiSetEAEvent()
                {
                    EmergencyAccess = !AirlockComp.EmergencyAccess,
                }
            });
        }
        if(TryComp<ElectrifiedComponent>(ent, out var ElectrifiedComp))
        {
            args.Actions.Add(new StationAiRadial()
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Misc/books.rsi"), "icon_lightning"),
                    
                OverlaySprite = ElectrifiedComp.Enabled ?
                    new SpriteSpecifier.Rsi(new ResPath("/Textures/Markers/cross.rsi"), "red") :
                    null,
                    
                Tooltip = ElectrifiedComp.Enabled ? Loc.GetString("de-electrify") : Loc.GetString("electrify"),
                Event = new StationAiElectrifyEvent()
                {
                    Electrified = !ElectrifiedComp.Enabled,
                }
            });
        }
    }
}

using Content.Shared.Silicons.StationAi;
using Content.Shared.Electrocution;
using Content.Shared.Database;
using Content.Shared.Doors;
using Content.Shared.Doors.Systems;
using Content.Shared.Doors.Components;
using Content.Server.Wires;
using Content.Shared.Power;

using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Silicons.StationAi;

public sealed partial class StationAiSystem
{
    [Dependency] private readonly WiresSystem _wiresSystem = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly ActorSystem _actorSystem = default!;

    private void InitializeAirlock()
    {
        SubscribeLocalEvent<AirlockComponent, StationAiElectrifyEvent>(OnAirlockElectrify);
    }

    public void OnAirlockElectrify(EntityUid ent, AirlockComponent component, StationAiElectrifyEvent args)
    {
        if(!TryComp<ElectrifiedComponent>(ent, out var comp))
        {
            return;
        }
        if(comp.Enabled == args.Electrified)
        {
            return;
        }
        if(_wiresSystem.TryGetData<int?>(ent, PowerWireActionKey.CutWires, out var cut) && cut > 0)
        {
            return;
        }
        if(TryComp<DoorComponent>(ent, out var doorComp) && !_doorSystem.HasAccess(ent, args.User, doorComp))
        {
            if(_actorSystem.TryGetSession(args.User, out var session) && session != null)
            {
                _popupSystem.PopupEntity(Loc.GetString("popup-deny-electrify"), ent, session);
            }
            return;
        }
        
        comp.Enabled = args.Electrified;
        Dirty(ent, comp);

        if(args.Electrified)
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"Station AI {ToPrettyString(args.User)} electrified {ToPrettyString(ent)}");
        else
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"Station AI {ToPrettyString(args.User)} de-electrified {ToPrettyString(ent)}");


        if(comp.Enabled)
        {
            RaiseNetworkEvent(new OnGotElectrifiedEvent() {nid = GetNetEntity(ent)});
            var sparks1sound = new SoundPathSpecifier("/Audio/Effects/sparks1.ogg");
            var sparks4sound = new SoundPathSpecifier("/Audio/Effects/sparks4.ogg");
            Audio.PlayPvs(sparks1sound, ent);
            Audio.PlayPvs(sparks4sound, ent, AudioParams.Default.WithPlayOffset(0.2f));
            _popupSystem.PopupEntity(Loc.GetString("popup-electrify"), ent);
        }
        else
        {
            var beep1sound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg");
            Audio.PlayPvs(beep1sound, ent);
            _popupSystem.PopupEntity(Loc.GetString("popup-de-electrify"), ent);
        }
    }
}
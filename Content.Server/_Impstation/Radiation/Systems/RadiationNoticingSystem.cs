using Content.Shared.Popups;
using Content.Shared.Radiation.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Server.Radiation.Components;
using System.Linq;

namespace Content.Server.Radiation.Systems;

public sealed class RadiationNoticingSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, OnIrradiatedEvent>(OnIrradiated);
    }

    private void OnIrradiated(EntityUid uid, ActorComponent actorComponent, OnIrradiatedEvent args)
    {
        // Roll chance for popup messages
        // This is per radiation update tick,
        // is tuned so that when being irradiated with 1 rad/sec, see 1 message every 5-ish seconds on average
        if (_random.NextFloat() <= args.RadsPerSecond / 20)
        {
            SendRadiationPopup(uid);
        }

        //TODO: Expand system with other effects: visual spots, vomiting blood?, blurry vision?
    }

    private void SendRadiationPopup(EntityUid uid){
        List<string> msgArr = [
                "radiation-noticing-message-0",
                "radiation-noticing-message-1",
                "radiation-noticing-message-2",
                "radiation-noticing-message-3",
                "radiation-noticing-message-4",
                "radiation-noticing-message-5",
                "radiation-noticing-message-6",
                "radiation-noticing-message-7"
            ];

        // Todo: detect possessing specific types of organs/blood/etc and conditionally add related messages to the list

        // pick a random message
        var msgId = _random.Pick(msgArr);
        var msg = Loc.GetString(msgId);

        // show it as a popup
        _popupSystem.PopupEntity(msg, uid, uid);
    }

}

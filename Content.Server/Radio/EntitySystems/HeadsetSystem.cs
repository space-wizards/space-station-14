using Content.Server.Chat.Systems;
using Content.Server.Radio.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.EntitySystems;

public sealed class HeadsetSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<HeadsetComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<HeadsetComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<WearingHeadsetComponent, EntitySpokeEvent>(OnSpeak);
    }

    private void OnSpeak(EntityUid uid, WearingHeadsetComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null
            && TryComp(component.Headset, out HeadsetComponent? headset)
            && headset.Channels.Contains(args.Channel.ID))
        {
            _radio.SendRadioMessage(uid, args.Message, args.Channel);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    private void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        component.IsEquipped = args.SlotFlags.HasFlag(component.RequiredSlot);

        if (component.IsEquipped && component.Enabled)
        {
            EnsureComp<WearingHeadsetComponent>(args.Equipee).Headset = uid;
            EnsureComp<ActiveRadioComponent>(uid).Channels.UnionWith(component.Channels);
        }
    }

    private void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        component.IsEquipped = false;
        RemComp<ActiveRadioComponent>(uid);
        RemComp<WearingHeadsetComponent>(args.Equipee);
    }

    public void SetEnabled(EntityUid uid, bool value, HeadsetComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Enabled == value)
            return;

        if (!value)
        {
            RemCompDeferred<ActiveRadioComponent>(uid);

            if (component.IsEquipped)
                RemCompDeferred<WearingHeadsetComponent>(Transform(uid).ParentUid);
        }
        else if (component.IsEquipped)
        {
            EnsureComp<WearingHeadsetComponent>(Transform(uid).ParentUid).Headset = uid;
            EnsureComp<ActiveRadioComponent>(uid).Channels.UnionWith(component.Channels);
        }
    }

    private void OnHeadsetReceive(EntityUid uid, HeadsetComponent component, RadioReceiveEvent args)
    {
        if (TryComp(Transform(uid).ParentUid, out ActorComponent? actor))
            _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.ConnectedClient);
    }

    private void OnExamined(EntityUid uid, HeadsetComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("examine-headset"));

        foreach (var id in component.Channels)
        {
            if (id == "Common") continue;

            var proto = _protoManager.Index<RadioChannelPrototype>(id);
            args.PushMarkup(Loc.GetString("examine-headset-channel",
                ("color", proto.Color),
                ("key", proto.KeyCode),
                ("id", proto.LocalizedName),
                ("freq", proto.Frequency)));
        }

        args.PushMarkup(Loc.GetString("examine-headset-chat-prefix", ("prefix", ";")));
    }
}

using Content.Server.Chat.Systems;
using Content.Server.Radio.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Content.Server.Tools;
using Content.Server.Popups;
using Robust.Shared.Player;
using Content.Shared.Tools.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class HeadsetSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<HeadsetComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<HeadsetComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<WearingHeadsetComponent, EntitySpokeEvent>(OnSpeak);

        SubscribeLocalEvent<HeadsetComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HeadsetComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<HeadsetComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<HeadsetComponent, EntRemovedFromContainerMessage>(OnContainerModified);
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
            PushRadioChannelsToOwner(uid, component);
        }
    }
    private void PushRadioChannelsToOwner(EntityUid uid, HeadsetComponent component)
    {
        EnsureComp<ActiveRadioComponent>(uid).Channels.UnionWith(component.Channels);
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
        if(!args.IsInDetailsRange)
            return;
        if(component.KeysInstalledAmount == 0)
        {
            args.PushMarkup(Loc.GetString("examine-headset-no-keys"));
        }
        // args.PushMarkup(Loc.GetString("examine-radio-frequency", ("frequency", component.BroadcastFrequency)));
        if(component.Channels.Count > 0)
        {
            args.PushMarkup(Loc.GetString("examine-headset"));
            foreach(var id in component.Channels)
            {
                // if(id == "Common")
                //     continue;
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


    private void OnStartup(EntityUid uid, HeadsetComponent component, ComponentStartup args)
    {
        component.KeyContainer = _container.EnsureContainer<Container>(uid, HeadsetComponent.KeyContainerName);
    }
    private bool InstallKey(HeadsetComponent src, EntityUid key)
    {
        if(src.KeyContainer.Insert(key))
        {
            ++src.KeysInstalledAmount;
            return true;
        }
        // RecalculateChannels(src); //handled by event
        return false;
    }
    private void RecalculateChannels(HeadsetComponent src)
    {
        src.Channels.Clear();
        src.KeysInstalledAmount = 0;
        foreach(EntityUid i in src.KeyContainer.ContainedEntities)
            if(TryComp<EncryptionKeyComponent?>(i, out var key))
            {
                ++src.KeysInstalledAmount;
                foreach(var j in key.Channels)
                    src.Channels.Add(j);
            }
        return;
    }
    private void OnInteractUsing(EntityUid uid, HeadsetComponent component, InteractUsingEvent args)
    {
        if(!component.IsKeysExtractable || !TryComp<ContainerManagerComponent>(uid, out var Storage))
        {
            return;
        }
       if(TryComp<EncryptionKeyComponent?>(args.Used, out var key))
        {
            if(component.KeySlotsAmount > component.KeysInstalledAmount)
                if(_container.TryRemoveFromContainer(args.Used) && InstallKey(component, args.Used))
                {
                    // RecalculateChannels(component);
                    _popupSystem.PopupEntity(Loc.GetString("headset-encryption-key-successfully-installed"), uid, Filter.Entities(args.User));
                    SoundSystem.Play(component.KeyInsertionSound.GetSound(), Filter.Pvs(args.Target), args.Target);
                }
            else
                _popupSystem.PopupEntity(Loc.GetString("headset-encryption-key-slots-already-full"), uid, Filter.Entities(args.User));
            return;
        } 
        if(TryComp<ToolComponent?>(args.Used, out var tool))
        {
            if(component.KeysInstalledAmount > 0)
            {
                if(_toolSystem.UseTool(
                    args.Used, args.User, uid,
                    0f, 0f, new String[] { "Screwing" },
                    doAfterCompleteEvent: null, toolComponent: tool)
                )
                {
                   var contained = new List<EntityUid>();
                    foreach (var i in component.KeyContainer.ContainedEntities)
                        contained.Add(i);

                    foreach (var i in contained)
                        if(TryComp<EncryptionKeyComponent>(i, out var _))
                            component.KeyContainer.Remove(i);
                    component.KeysInstalledAmount = 0;
                    // RecalculateChannels(component);
                    _popupSystem.PopupEntity(Loc.GetString("headset-encryption-keys-all-extrated"), uid, Filter.Entities(args.User));
                    SoundSystem.Play(component.KeyExtractionSound.GetSound(), Filter.Pvs(args.Target), args.Target);
                }
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("headset-encryption-keys-no-keys"), uid, Filter.Entities(args.User));

            }
        }
    }
    private void OnContainerModified(EntityUid uid, HeadsetComponent component, ContainerModifiedMessage args)
    {
        RecalculateChannels(component);
        PushRadioChannelsToOwner(uid, component);
    }
}

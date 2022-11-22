using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Radio.Components;
using Content.Server.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Radio.EntitySystems;

public sealed class HeadsetSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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
            PushRadioChannelsToOwner(uid, component, EnsureComp<ActiveRadioComponent>(uid));
        }
    }
    private void PushRadioChannelsToOwner(EntityUid uid, HeadsetComponent component, ActiveRadioComponent activeRadio)
    {
        activeRadio.Channels.UnionWith(component.Channels);
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
        if (component.KeyContainer.ContainedEntities.Count == 0)
        {
            args.PushMarkup(Loc.GetString("examine-headset-no-keys"));
            return;
        }
        else if (component.Channels.Count > 0)
        {
            args.PushMarkup(Loc.GetString("examine-headset"));
            foreach (var id in component.Channels)
            {
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

    private bool InstallKey(HeadsetComponent src, EntityUid key, EncryptionKeyComponent keyComponent)
    {
        if (src.KeyContainer.Insert(key))
        {
            UploadChannelsFromKey(src, keyComponent);
            return true;
        }
        return false;
    }
    private void UploadChannelsFromKey(HeadsetComponent src, EncryptionKeyComponent key)
    {
        foreach (var j in key.Channels)
            src.Channels.Add(j);
    }
    private void RecalculateChannels(HeadsetComponent src)
    {
        src.Channels.Clear();
        foreach (EntityUid i in src.KeyContainer.ContainedEntities)
            if (TryComp<EncryptionKeyComponent>(i, out var key))
                UploadChannelsFromKey(src, key);
    }

    private void OnInteractUsing(EntityUid uid, HeadsetComponent component, InteractUsingEvent args)
    {
        if (!component.IsKeysExtractable || !TryComp<ContainerManagerComponent>(uid, out var storage))
        {
            return;
        }
        if (TryComp<EncryptionKeyComponent>(args.Used, out var key))
        {
            if (component.KeySlotsAmount > component.KeyContainer.ContainedEntities.Count)
                if (_container.TryRemoveFromContainer(args.Used) && InstallKey(component, args.Used, key))
                {
                    _popupSystem.PopupEntity(Loc.GetString("headset-encryption-key-successfully-installed"), uid, Filter.Entities(args.User));
                    _audio.PlayPvs(_audio.GetSound(component.KeyInsertionSound), args.Target);
                }
            else
                _popupSystem.PopupEntity(Loc.GetString("headset-encryption-key-slots-already-full"), uid, Filter.Entities(args.User));
        } 
        if (TryComp<ToolComponent>(args.Used, out var tool))
        {
            if (component.KeyContainer.ContainedEntities.Count > 0)
                if (_toolSystem.UseTool(
                    args.Used, args.User, uid,
                    0f, 0f, new String[] {"Screwing"},
                    doAfterCompleteEvent: null, toolComponent: tool)
                )
                {
                    var contained = new List<EntityUid>();
                    foreach (var i in component.KeyContainer.ContainedEntities)
                        contained.Add(i);
                    foreach (var i in contained)
                        if (HasComp<EncryptionKeyComponent>(i))
                            component.KeyContainer.Remove(i);
                    component.Channels.Clear();

                    _popupSystem.PopupEntity(Loc.GetString("headset-encryption-keys-all-extrated"), uid, Filter.Entities(args.User));
                    _audio.PlayPvs(_audio.GetSound(component.KeyExtractionSound), args.Target);
                }
            else
                _popupSystem.PopupEntity(Loc.GetString("headset-encryption-keys-no-keys"), uid, Filter.Entities(args.User));
        }
    }

    private void OnContainerModified(EntityUid uid, HeadsetComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID == HeadsetComponent.KeyContainerName)
            if(args.Container.ContainedEntities.Contains(args.Entity))
                if (TryComp<EncryptionKeyComponent>(args.Entity, out var added))
                {
                    UploadChannelsFromKey(component, added);
                    PushRadioChannelsToOwner(uid, component, EnsureComp<ActiveRadioComponent>(uid));
                }
            else
                RecalculateChannels(component);
    }
}

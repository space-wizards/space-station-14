using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Content.Server.Radio.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Network;
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
        SubscribeLocalEvent<HeadsetComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
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
            UpdateRadioChannelsInActiveRadio(uid, component, EnsureComp<ActiveRadioComponent>(uid));
        }
    }

    private void UpdateRadioChannelsInActiveRadio(EntityUid uid, HeadsetComponent component, ActiveRadioComponent activeRadio)
    {
        activeRadio.Channels.Clear();
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
            args.PushMarkup(Loc.GetString("examine-headset-channels-prefix"));
            EncryptionKeySystem.GetChannelsExamine(component.Channels, args, _protoManager, "examine-headset-channel");
            args.PushMarkup(Loc.GetString("examine-headset-chat-prefix", ("prefix", ":h")));
            if (component.DefaultChannel != null)
            {
                var proto = _protoManager.Index<RadioChannelPrototype>(component.DefaultChannel);
                args.PushMarkup(Loc.GetString("examine-headset-default-channel", ("channel", component.DefaultChannel), ("color", proto.Color)));
            }
        }
    }

    private void OnStartup(EntityUid uid, HeadsetComponent component, ComponentStartup args)
    {
        component.KeyContainer = _container.EnsureContainer<Container>(uid, HeadsetComponent.KeyContainerName);
    }

    private bool InstallKey(HeadsetComponent component, EntityUid key, EncryptionKeyComponent keyComponent)
    {
        return component.KeyContainer.Insert(key);
    }

    private void UploadChannelsFromKey(HeadsetComponent component, EncryptionKeyComponent key)
    {
        foreach (var j in key.Channels)
            component.Channels.Add(j);
    }

    public void RecalculateChannels(HeadsetComponent component)
    {
        component.Channels.Clear();
        foreach (EntityUid i in component.KeyContainer.ContainedEntities)
        {
            if (TryComp<EncryptionKeyComponent>(i, out var key))
            {
                UploadChannelsFromKey(component, key);
            }
        }
    }

    private void OnInteractUsing(EntityUid uid, HeadsetComponent component, InteractUsingEvent args)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var storage))
            return;
        if(!component.IsKeysUnlocked)
        {
            _popupSystem.PopupEntity(Loc.GetString("headset-encryption-keys-are-locked"), uid, args.User);
            return;
        }
        if (TryComp<EncryptionKeyComponent>(args.Used, out var key))
        {
            if (component.KeySlots > component.KeyContainer.ContainedEntities.Count)
            {
                if (InstallKey(component, args.Used, key))
                {                    
                    _popupSystem.PopupEntity(Loc.GetString("headset-encryption-key-successfully-installed"), uid, args.User);
                    _audio.PlayPvs(component.KeyInsertionSound, args.Target);
                }
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("headset-encryption-key-slots-already-full"), uid, args.User);
            }
        }
        if (TryComp<ToolComponent>(args.Used, out var tool))
        {
            if (component.KeyContainer.ContainedEntities.Count > 0)
            {
                if (_toolSystem.UseTool(
                    args.Used, args.User, uid,
                    0f, 0f, new String[] { component.KeysExtractionMethod },
                    doAfterCompleteEvent: null, toolComponent: tool)
                )
                {
                    var contained = component.KeyContainer.ContainedEntities.ToArray<EntityUid>();
                    foreach (var i in contained)
                        component.KeyContainer.Remove(i);
                    component.Channels.Clear();
                    UpdateDefaultChannel(component);
                    _popupSystem.PopupEntity(Loc.GetString("headset-encryption-keys-all-extracted"), uid, args.User);
                    _audio.PlayPvs(component.KeyExtractionSound, args.Target);
                }
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("headset-encryption-keys-no-keys"), uid, args.User);
            }
        }
    }

    private void UpdateDefaultChannel(HeadsetComponent component)
    {
        if (component.KeyContainer.ContainedEntities.Count >= 1)
            component.DefaultChannel = EnsureComp<EncryptionKeyComponent>(component.KeyContainer.ContainedEntities[0])?.DefaultChannel;
        else
            component.DefaultChannel = null;
    }

    private void OnContainerInserted(EntityUid uid, HeadsetComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != HeadsetComponent.KeyContainerName)
        {
            return;
        }
        if (TryComp<EncryptionKeyComponent>(args.Entity, out var added))
        {
            UpdateDefaultChannel(component);
            UploadChannelsFromKey(component, added);
            UpdateRadioChannelsInActiveRadio(uid, component, EnsureComp<ActiveRadioComponent>(uid));
        }
        return;
    }
}

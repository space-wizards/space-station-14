using System.Linq;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Shared.Radio.EntitySystems;

/// <summary>
///     This system manages encryption keys & key holders for use with radio channels.
/// </summary>
public sealed partial class EncryptionKeySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EncryptionKeyComponent, ExaminedEvent>(OnKeyExamined);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, ExaminedEvent>(OnHolderExamined);

        SubscribeLocalEvent<EncryptionKeyHolderComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<EncryptionKeyHolderComponent, EncryptionRemovalFinishedEvent>(OnKeyRemoval);
    }

    private void OnKeyRemoval(EntityUid uid, EncryptionKeyHolderComponent component, EncryptionRemovalFinishedEvent args)
    {
        if (args.Cancelled)
            return;

        var contained = component.KeyContainer.ContainedEntities.ToArray();
        _container.EmptyContainer(component.KeyContainer, reparent: false);
        foreach (var ent in contained)
        {
            _hands.PickupOrDrop(args.User, ent);
        }

        if (!_timing.IsFirstTimePredicted)
            return;

        // TODO add predicted pop-up overrides.
        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("encryption-keys-all-extracted"), uid, args.User);

        _audio.PlayPredicted(component.KeyExtractionSound, uid, args.User);
    }

    public void UpdateChannels(EntityUid uid, EncryptionKeyHolderComponent component)
    {
        if (!component.Initialized)
            return;

        component.Channels.Clear();
        component.DefaultChannel = null;

        foreach (var ent in component.KeyContainer.ContainedEntities)
        {
            if (TryComp<EncryptionKeyComponent>(ent, out var key))
            {
                component.Channels.UnionWith(key.Channels);
                component.DefaultChannel ??= key.DefaultChannel;
            }
        }

        RaiseLocalEvent(uid, new EncryptionChannelsChangedEvent(component));
    }

    private void OnContainerModified(EntityUid uid, EncryptionKeyHolderComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID == EncryptionKeyHolderComponent.KeyContainerName)
            UpdateChannels(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, EncryptionKeyHolderComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<EncryptionKeyComponent>(args.Used))
        {
            args.Handled = true;
            TryInsertKey(uid, component, args);
        }
        else if (TryComp<ToolComponent>(args.Used, out var tool)
                 && tool.Qualities.Contains(component.KeysExtractionMethod)
                 && component.KeyContainer.ContainedEntities.Count > 0) // dont block deconstruction
        {
            args.Handled = true;
            TryRemoveKey(uid, component, args, tool);
        }
    }

    private void TryInsertKey(EntityUid uid, EncryptionKeyHolderComponent component, InteractUsingEvent args)
    {
        if (!component.KeysUnlocked)
        {
            _popup.PopupClient(Loc.GetString("encryption-keys-are-locked"), uid, args.User);
            return;
        }

        if (TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
        {
            _popup.PopupClient(Loc.GetString("encryption-keys-panel-locked"), uid, args.User);
            return;
        }

        if (component.KeySlots <= component.KeyContainer.ContainedEntities.Count)
        {
            _popup.PopupClient(Loc.GetString("encryption-key-slots-already-full"), uid, args.User);
            return;
        }

        if (_container.Insert(args.Used, component.KeyContainer))
        {
            _popup.PopupClient(Loc.GetString("encryption-key-successfully-installed"), uid, args.User);
            _audio.PlayPredicted(component.KeyInsertionSound, args.Target, args.User);
            args.Handled = true;
            return;
        }
    }

    private void TryRemoveKey(EntityUid uid, EncryptionKeyHolderComponent component, InteractUsingEvent args,
        ToolComponent? tool)
    {
        if (!component.KeysUnlocked)
        {
            _popup.PopupClient(Loc.GetString("encryption-keys-are-locked"), uid, args.User);
            return;
        }

        if (TryComp<WiresPanelComponent>(uid, out var panel) && !panel.Open)
        {
            _popup.PopupClient(Loc.GetString("encryption-keys-panel-locked"), uid, args.User);
            return;
        }

        if (component.KeyContainer.ContainedEntities.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("encryption-keys-no-keys"), uid, args.User);
            return;
        }

        _tool.UseTool(args.Used, args.User, uid, 1f, component.KeysExtractionMethod, new EncryptionRemovalFinishedEvent(), toolComponent: tool);
    }

    private void OnStartup(EntityUid uid, EncryptionKeyHolderComponent component, ComponentStartup args)
    {
        component.KeyContainer = _container.EnsureContainer<Container>(uid, EncryptionKeyHolderComponent.KeyContainerName);
        UpdateChannels(uid, component);
    }

    private void OnHolderExamined(EntityUid uid, EncryptionKeyHolderComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (component.KeyContainer.ContainedEntities.Count == 0)
        {
            args.PushMarkup(Loc.GetString("encryption-keys-no-keys"));
            return;
        }

        if (component.Channels.Count > 0)
        {
            args.PushMarkup(Loc.GetString("examine-encryption-channels-prefix"));
            AddChannelsExamine(component.Channels, component.DefaultChannel, args, _protoManager, "examine-encryption-channel");
        }
    }

    private void OnKeyExamined(EntityUid uid, EncryptionKeyComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if(component.Channels.Count > 0)
        {
            args.PushMarkup(Loc.GetString("examine-encryption-channels-prefix"));
            AddChannelsExamine(component.Channels, component.DefaultChannel, args, _protoManager, "examine-encryption-channel");
        }
    }

    /// <summary>
    ///     A method for formating list of radio channels for examine events.
    /// </summary>
    /// <param name="channels">HashSet of channels in headset, encryptionkey or etc.</param>
    /// <param name="protoManager">IPrototypeManager for getting prototypes of channels with their variables.</param>
    /// <param name="channelFTLPattern">String that provide id of pattern in .ftl files to format channel with variables of it.</param>
    public void AddChannelsExamine(HashSet<string> channels, string? defaultChannel, ExaminedEvent examineEvent, IPrototypeManager protoManager, string channelFTLPattern)
    {
        RadioChannelPrototype? proto;
        foreach (var id in channels)
        {
            proto = _protoManager.Index<RadioChannelPrototype>(id);

            var key = id == SharedChatSystem.CommonChannel
                ? SharedChatSystem.RadioCommonPrefix.ToString()
                : $"{SharedChatSystem.RadioChannelPrefix}{proto.KeyCode}";

            examineEvent.PushMarkup(Loc.GetString(channelFTLPattern,
                ("color", proto.Color),
                ("key", key),
                ("id", proto.LocalizedName),
                ("freq", proto.Frequency)));
        }

        if (defaultChannel != null && _protoManager.TryIndex(defaultChannel, out proto))
        {
            if (HasComp<HeadsetComponent>(examineEvent.Examined))
            {
                var msg = Loc.GetString("examine-headset-default-channel",
                ("prefix", SharedChatSystem.DefaultChannelPrefix),
                ("channel", defaultChannel),
                ("color", proto.Color));
                examineEvent.PushMarkup(msg);
            }
            if (HasComp<EncryptionKeyComponent>(examineEvent.Examined))
            {
                var msg = Loc.GetString("examine-encryption-default-channel",
                ("channel", defaultChannel),
                ("color", proto.Color));
                examineEvent.PushMarkup(msg);
            }
        }
    }

    [Serializable, NetSerializable]
    public sealed partial class EncryptionRemovalFinishedEvent : SimpleDoAfterEvent
    {
    }
}

using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Radio.EntitySystems;

/// <summary>
///     This system manages encryption keys & key holders for use with radio channels.
/// </summary>
public sealed class EncryptionKeySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
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
        if (!TryComp<ContainerManagerComponent>(uid, out var storage))
            return;

        if (TryComp<EncryptionKeyComponent>(args.Used, out var key))
        {
            args.Handled = true;

            if (!component.KeysUnlocked)
            {
                if (_timing.IsFirstTimePredicted)
                    _popupSystem.PopupEntity(Loc.GetString("headset-encryption-keys-are-locked"), uid, Filter.Local(), false);
                return;
            }

            if (component.KeySlots <= component.KeyContainer.ContainedEntities.Count)
            {
                if (_timing.IsFirstTimePredicted)
                    _popupSystem.PopupEntity(Loc.GetString("headset-encryption-key-slots-already-full"), uid, Filter.Local(), false);
                return;
            }

            if (component.KeyContainer.Insert(args.Used))
            {
                if (_timing.IsFirstTimePredicted)
                    _popupSystem.PopupEntity(Loc.GetString("headset-encryption-key-successfully-installed"), uid, Filter.Local(), false);
                _audio.PlayPredicted(component.KeyInsertionSound, args.Target, args.User);
                return;
            }
        }

        if (!TryComp<ToolComponent>(args.Used, out var tool) || !tool.Qualities.Contains(component.KeysExtractionMethod))
            return;

        args.Handled = true;

        if (component.KeyContainer.ContainedEntities.Count == 0)
        {
            if (_timing.IsFirstTimePredicted)
                _popupSystem.PopupEntity(Loc.GetString("headset-encryption-keys-no-keys"), uid, Filter.Local(), false);
            return;
        }

        if (!_toolSystem.UseTool(args.Used, args.User, uid, 0f, 0f, component.KeysExtractionMethod, toolComponent: tool))
            return;

        var contained = component.KeyContainer.ContainedEntities.ToArray();
        _container.EmptyContainer(component.KeyContainer, entMan: EntityManager);
        foreach (var ent in contained)
        {
            _hands.PickupOrDrop(args.User, ent);
        }

        // if tool use ever gets predicted this needs changing.
        _popupSystem.PopupEntity(Loc.GetString("headset-encryption-keys-all-extracted"), uid, args.User);
        _audio.PlayPvs(component.KeyExtractionSound, args.Target);
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
            args.PushMarkup(Loc.GetString("examine-headset-no-keys"));
            return;
        }

        if (component.Channels.Count > 0)
        {
            args.PushMarkup(Loc.GetString("examine-headset-channels-prefix"));
            AddChannelsExamine(component.Channels, component.DefaultChannel, args, _protoManager, "examine-headset-channel");
        }
    }

    private void OnKeyExamined(EntityUid uid, EncryptionKeyComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if(component.Channels.Count > 0)
        {
            args.PushMarkup(Loc.GetString("examine-encryption-key-channels-prefix"));
            AddChannelsExamine(component.Channels, component.DefaultChannel, args, _protoManager, "examine-headset-channel");
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
            proto = protoManager.Index<RadioChannelPrototype>(id);

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
            var msg = Loc.GetString("examine-default-channel",
                ("prefix", SharedChatSystem.DefaultChannelPrefix),
                ("channel", defaultChannel),
                ("color", proto.Color));
            examineEvent.PushMarkup(msg);
        }
    }
}

using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared.Radio.EntitySystems;

public sealed class EncryptionKeyChannelsCopySystem : EntitySystem
{
    [Dependency] private readonly EncryptionKeySystem _encryptionKey = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EncryptionKeyChannelsCopyComponent, BeforeRangedInteractEvent>(OnBeforeInteractWith);
    }

    /// <summary>
    /// Tries to get channels from <paramref name="copyFrom"/> and when copies it to <paramref name="receiver"/>.
    /// </summary>
    /// <param name="copyFrom"> Can be entity with <see cref="EncryptionKeyHolderComponent"/> or <see cref="EncryptionKeyComponent"/>. </param>
    /// <returns>
    /// False if entity dont have <see cref="EncryptionKeyChannelsCopyComponent"/> and <see cref="EncryptionKeyComponent"/>  or <paramref name="copyFrom"/> dont have any components which has radio channels.
    /// </returns>
    public bool TryCopyChannels(Entity<EncryptionKeyChannelsCopyComponent?, EncryptionKeyComponent?> receiver, EntityUid copyFrom)
    {
        if (!Resolve(receiver.Owner, ref receiver.Comp1, false))
            return false;

        if (!Resolve(receiver.Owner, ref receiver.Comp2, false))
            return false;

        if (TryComp<EncryptionKeyHolderComponent>(copyFrom, out var keyHolderComponent))
        {
            // make possible to insert key in a headset
            if (keyHolderComponent.KeyContainer.Count == 0)
                return false;

            AddChannelsAndPopup((receiver.Owner, receiver.Comp2), keyHolderComponent.Channels, copyFrom);
            return true;
        }

        if (TryComp<EncryptionKeyComponent>(copyFrom, out var keyComponent))
        {
            AddChannelsAndPopup((receiver.Owner, receiver.Comp2), keyComponent.Channels, copyFrom);
            return true;
        }

        return false;
    }

    private void OnBeforeInteractWith(Entity<EncryptionKeyChannelsCopyComponent> entity, ref BeforeRangedInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach)
            return;

        if (!TryCopyChannels((entity.Owner, entity.Comp), args.Target.Value))
            RelayFromKeyHolder(entity.Owner, ref args);

        args.Handled = true;
    }

    private void AddChannelsAndPopup(Entity<EncryptionKeyComponent> receiver, HashSet<string> channels, EntityUid sourceUid)
    {
        if (_encryptionKey.TryAddChannels(receiver, channels))
            _popup.PopupCursor(Loc.GetString("encryption-key-channel-copy-successful", ("used", receiver.Owner), ("target", sourceUid)));
        else
            _popup.PopupCursor(Loc.GetString("encryption-key-channel-copy-failed", ("used", receiver.Owner), ("target", sourceUid)));
    }

    private void RelayFromKeyHolder<T>(Entity<EncryptionKeyHolderComponent?> entity, ref T args) where T : notnull
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        foreach (var encryptionKey in entity.Comp.KeyContainer.ContainedEntities)
        {
            RaiseLocalEvent(encryptionKey, ref args);
        }
    }
}

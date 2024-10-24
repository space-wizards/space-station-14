using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.BroadcastInteractionUsingToContainer;
using Content.Shared.Interaction;

namespace Content.Shared.Radio.EntitySystems;

public sealed partial class EncryptionKeyChannelsCopySystem : EntitySystem
{
    [Dependency] private readonly EncryptionKeySystem _encryptionKey = default!;
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EncryptionKeyChannelsCopyComponent, InteractBeforeUsingFromContainerEvent>(OnInteractUsingInContainer);
        SubscribeLocalEvent<EncryptionKeyChannelsCopyComponent, BeforeRangedInteractEvent>(OnBeforeInteractWith);
    }

    /// <summary>
    /// Tries to get channels from <paramref name="sourceUid"/> and when copies it to <paramref name="receiver"/>.
    /// </summary>
    /// <param name="sourceUid"> Can be entity with <see cref="EncryptionKeyHolderComponent"/> or <see cref="EncryptionKeyComponent"/>. </param>
    /// <returns>
    /// False if entity dont have <see cref="EncryptionKeyChannelsCopyComponent"/> and <see cref="EncryptionKeyComponent"/>  or <paramref name="sourceUid"/> dont have any components which has radio channels.
    /// </returns>
    public bool TryCopyChannels(Entity<EncryptionKeyChannelsCopyComponent?, EncryptionKeyComponent?> receiver, EntityUid sourceUid)
    {
        if (!Resolve(receiver.Owner, ref receiver.Comp1))
            return false;

        if (!Resolve(receiver.Owner, ref receiver.Comp2))
            return false;

        if (TryComp<EncryptionKeyHolderComponent>(sourceUid, out var keyHolderComponent))
        {
            // make possible to insert key in a headset
            if (keyHolderComponent.KeyContainer.Count == 0)
                return false;

            AddChannelsAndPopup((receiver.Owner, receiver.Comp2), keyHolderComponent.Channels, sourceUid);
            return true;
        }

        if (TryComp<EncryptionKeyComponent>(sourceUid, out var keyComponent))
        {
            AddChannelsAndPopup((receiver.Owner, receiver.Comp2), keyComponent.Channels, sourceUid);
            return true;
        }

        return false;
    }

    private void OnInteractUsingInContainer(Entity<EncryptionKeyChannelsCopyComponent> ent, ref InteractBeforeUsingFromContainerEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach)
            return;

        if (TryCopyChannels((ent.Owner, ent.Comp), args.Target.Value))
            args.Handled = true;
    }

    private void OnBeforeInteractWith(Entity<EncryptionKeyChannelsCopyComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach)
            return;

        if (TryCopyChannels((ent.Owner, ent.Comp), args.Target.Value))
            args.Handled = true;
    }

    private void AddChannelsAndPopup(Entity<EncryptionKeyComponent> receiver, HashSet<string> channels, EntityUid sourceUid)
    {
        if (_encryptionKey.AddChannels(receiver, channels))
            _popup.PopupCursor(_localization.GetString("encryption-key-channel-copy-successful", ("used", receiver.Owner), ("target", sourceUid)));
        else
            _popup.PopupCursor(_localization.GetString("encryption-key-channel-copy-failed", ("used", receiver.Owner), ("target", sourceUid)));
    }
}

using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Materials;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.ParcelWrap.Systems;

// This part handles Wrapped Parcels
public sealed partial class ParcelWrappingSystem
{
    private void InitializeWrappedParcel()
    {
        SubscribeLocalEvent<WrappedParcelComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<WrappedParcelComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<WrappedParcelComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbsForWrappedParcel);
        SubscribeLocalEvent<WrappedParcelComponent, UnwrapWrappedParcelDoAfterEvent>(OnUnwrapParcelDoAfter);
        SubscribeLocalEvent<WrappedParcelComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<WrappedParcelComponent, GotReclaimedEvent>(OnDestroyed);
    }

    private void OnComponentInit(Entity<WrappedParcelComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Contents = _container.EnsureContainer<ContainerSlot>(entity, entity.Comp.ContainerId);
    }

    private void OnUseInHand(Entity<WrappedParcelComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartUnwrapDoAfter(args.User, entity);
    }

    private void OnGetVerbsForWrappedParcel(Entity<WrappedParcelComponent> entity,
        ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess)
            return;

        // "Capture" the values from `args` because C# doesn't like doing the capturing for `ref` values.
        var user = args.User;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("parcel-wrap-verb-unwrap"),
            Act = () => TryStartUnwrapDoAfter(user, entity),
        });
    }

    private void OnUnwrapParcelDoAfter(Entity<WrappedParcelComponent> entity, ref UnwrapWrappedParcelDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is { } target && TryComp<WrappedParcelComponent>(target, out var parcel))
        {
            UnwrapInternal(args.User, (target, parcel));
            args.Handled = true;
        }
    }

    private void OnDestroyed<T>(Entity<WrappedParcelComponent> parcel, ref T args)
    {
        // Unwrap the package and if something was in it, show a popup describing "wow something came out!"
        if (UnwrapInternal(user: null, parcel) is { } contents)
        {
            _popup.PopupPredicted(Loc.GetString("parcel-wrap-popup-parcel-destroyed", ("contents", contents)),
                contents,
                null,
                PopupType.MediumCaution);
        }
    }

    private bool TryStartUnwrapDoAfter(EntityUid user, Entity<WrappedParcelComponent> parcel)
    {
        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            parcel.Comp.UnwrapDelay,
            new UnwrapWrappedParcelDoAfterEvent(),
            parcel,
            parcel)
        {
            NeedHand = true,
        });
    }

    /// <summary>
    /// Despawns <paramref name="parcel"/>, leaving the contained entity where the parcel was.
    /// </summary>
    /// <param name="user">The entity doing the unwrapping.</param>
    /// <param name="parcel">The entity being unwrapped.</param>
    /// <returns>
    /// The newly unwrapped, contained entity. Returns null only in the exceptional case that the parcel contained
    /// nothing, which should be prevented by not creating such parcels.
    /// </returns>
    private EntityUid? UnwrapInternal(EntityUid? user, Entity<WrappedParcelComponent> parcel)
    {
        var containedEntity = parcel.Comp.Contents.ContainedEntity;
        _audio.PlayPredicted(parcel.Comp.UnwrapSound, parcel, user);

        // If we're on the client, just return the contained entity and don't try to despawn the parcel.
        if (!_net.IsServer)
            return containedEntity;

        var parcelTransform = Transform(parcel);

        if (containedEntity is { } parcelContents)
        {
            _container.Remove(parcelContents,
                parcel.Comp.Contents,
                true,
                true,
                parcelTransform.Coordinates);

            // If the parcel is in a container, try to put the unwrapped contents in that container.
            if (_container.TryGetContainingContainer((parcel, null, null), out var outerContainer))
            {
                // Make space in the container for the parcel contents.
                _container.Remove((parcel, null, null), outerContainer, force: true);
                _container.InsertOrDrop((parcelContents, null, null), outerContainer);
            }
        }

        // Spawn unwrap trash.
        if (parcel.Comp.UnwrapTrash is { } trashProto)
        {
            var trash = Spawn(trashProto, parcelTransform.Coordinates);
            _transform.DropNextTo((trash, null), (parcel, parcelTransform));
        }

        QueueDel(parcel);

        return containedEntity;
    }
}

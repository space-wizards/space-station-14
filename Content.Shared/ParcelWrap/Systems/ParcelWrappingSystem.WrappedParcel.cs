using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Materials;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Shared.ParcelWrap.Systems;

// This part handles Wrapped Parcels
public abstract partial class SharedParcelWrappingSystem
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
        entity.Comp.Contents = Container.EnsureContainer<ContainerSlot>(entity, entity.Comp.ContainerId);
    }

    private void OnUseInHand(Entity<WrappedParcelComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        TryStartUnwrapDoAfter(args.User, entity);
        args.Handled = true;
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


    private bool TryStartUnwrapDoAfter(EntityUid user, Entity<WrappedParcelComponent> parcel) =>
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            parcel.Comp.UnwrapDelay,
            new UnwrapWrappedParcelDoAfterEvent(),
            parcel,
            parcel)
        {
            NeedHand = true,
        });

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
        var parcelTransform = Transform(parcel);

        var containedEntity = parcel.Comp.Contents.ContainedEntity;
        if (containedEntity is { } parcelContents)
        {
            Container.Remove(parcelContents,
                parcel.Comp.Contents,
                true,
                true,
                parcelTransform.Coordinates);

            // If the parcel is in a container, try to put the unwrapped contents in that container.
            if (Container.TryGetContainingContainer((parcel, null, null), out var outerContainer))
            {
                // Make space in the container for the parcel contents.
                Container.Remove((parcel, null, null), outerContainer, force: true);
                Container.InsertOrDrop((parcelContents, null, null), outerContainer);
            }
        }

        // Make some trash and play an unwrapping sound.
        SpawnUnwrapTrash((parcel, parcel.Comp, parcelTransform));
        _audio.PlayPredicted(parcel.Comp.UnwrapSound, parcel, user);

        Del(parcel);

        return containedEntity;
    }

    /// <remarks>
    /// Split off from <see cref="UnwrapInternal"/> so that entity spawning is only performed on the server.
    /// </remarks>
    protected abstract void SpawnUnwrapTrash(Entity<WrappedParcelComponent, TransformComponent> parcel);
}

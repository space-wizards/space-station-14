using Content.Server.ParcelWrap.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Server.ParcelWrap.EntitySystems;

// This part handles Wrapped Parcels
public sealed partial class ParcelWrappingSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private void OnComponentInit(Entity<WrappedParcelComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Contents = _container.EnsureContainer<ContainerSlot>(entity, WrappedParcelComponent.ContainerId);
    }

    private void OnUseInHand(Entity<WrappedParcelComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        UnwrapInternal(entity);
        args.Handled = true;
    }

    private void OnGetVerbsForWrappedParcel(Entity<WrappedParcelComponent> entity,
        ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess)
            return;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("parcel-wrap-verb-unwrap"),
            Act = () => UnwrapInternal(entity),
        });
    }

    private void OnDestroyed<T>(Entity<WrappedParcelComponent> parcel, ref T args)
    {
        // Unwrap the package and if something was in it, show a popup describing "wow something came out!"
        if (UnwrapInternal(parcel) is { } contents)
        {
            _popup.PopupPredicted(Loc.GetString("parcel-wrap-popup-parcel-destroyed", ("contents", contents)),
                contents,
                null,
                PopupType.MediumCaution);
        }
    }
}

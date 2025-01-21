using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.ParcelWrap.EntitySystems;

// This part handles Wrapped Parcels
public abstract partial class SharedParcelWrappingSystem
{
    private void OnComponentInit(Entity<WrappedParcelComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Contents = _container.EnsureContainer<ContainerSlot>(entity, WrappedParcelComponent.ContainerId);
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
}

[Serializable, NetSerializable]
public sealed partial class UnwrapWrappedParcelDoAfterEvent : SimpleDoAfterEvent;

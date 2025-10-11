using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ParcelWrap.Systems;

// This part handles Parcel Wrap.
public sealed partial class ParcelWrappingSystem
{
    [Dependency]
    private readonly IPrototypeManager _proto = default!;

    private static readonly EntProtoId<WrappedParcelComponent> DefaultWrappedParcel = "WrappedParcel";
    private static ProtoId<ItemSizePrototype> _fallbackParcelSize = "Ginormous";

    private void InitializeParcelWrap()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);

        SubscribeLocalEvent<ParcelWrapComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ParcelWrapComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbsForParcelWrap);
        SubscribeLocalEvent<ParcelWrapComponent, ParcelWrapItemDoAfterEvent>(OnWrapItemDoAfter);

        SetFallbackParcelSize();
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ItemSizePrototype>())
            SetFallbackParcelSize();
    }

    private void SetFallbackParcelSize()
    {
        if (_proto.EnumeratePrototypes<ItemSizePrototype>().Max() is { } size)
        {
            _fallbackParcelSize = size;
        }
    }

    private void OnAfterInteract(Entity<ParcelWrapComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            args.Target is not { } target ||
            !args.CanReach ||
            !IsWrappable(entity, target))
            return;

        args.Handled = TryStartWrapDoAfter(args.User, entity, target);
    }

    private void OnGetVerbsForParcelWrap(Entity<ParcelWrapComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !IsWrappable(entity, args.Target))
            return;

        // "Capture" the values from `args` because C# doesn't like doing the capturing for `ref` values.
        var user = args.User;
        var target = args.Target;

        // "Wrap" verb for when just left-clicking doesn't work.
        args.Verbs.Add(new UtilityVerb
        {
            Text = Loc.GetString("parcel-wrap-verb-wrap"),
            Act = () => TryStartWrapDoAfter(user, entity, target),
        });
    }

    private void OnWrapItemDoAfter(Entity<ParcelWrapComponent> wrapper, ref ParcelWrapItemDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is { } target)
        {
            Wrap(args.User, wrapper, target);
            args.Handled = true;
        }
    }

    private bool TryStartWrapDoAfter(EntityUid user, Entity<ParcelWrapComponent> wrapper, EntityUid target)
    {
        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            wrapper.Comp.WrapDelay,
            new ParcelWrapItemDoAfterEvent(),
            wrapper, // Raise the event on the wrapper because that's what the event handler expects.
            target,
            wrapper)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnDamage = true,
        });
    }

    /// <summary>
    /// Spawns a WrappedParcel containing <paramref name="target"/>.
    /// </summary>
    /// <param name="user">The entity using <paramref name="wrapper"/> to wrap <paramref name="target"/>.</param>
    /// <param name="wrapper">The wrapping being used. Determines appearance of the spawned parcel.</param>
    /// <param name="target">The entity being wrapped.</param>
    private void Wrap(EntityUid user,
        Entity<ParcelWrapComponent> wrapper,
        EntityUid target
    )
    {
        var parcel = Wrap(
            target,
            wrapper.Comp.ParcelPrototype,
            wrapper.Comp.WrappedItemsMaintainSize,
            wrapper.Comp.WrappedItemsMaintainShape
        );
        if (parcel is null)
            return;

        // Consume a `use` on the wrapper, and delete the wrapper if it's empty.
        _charges.TryUseCharges(wrapper.Owner, 1);
        if (_charges.IsEmpty(wrapper.Owner))
            PredictedQueueDel(wrapper);

        // Play a wrapping sound.
        _audio.PlayPredicted(wrapper.Comp.WrapSound, target, user);
    }

    /// <summary>
    /// Wraps <paramref name="toWrap"/> into a parcel. This spawns <paramref name="parcelProto"/>
    /// (or <see cref="DefaultWrappedParcel"/>) and inserts <paramref name="toWrap"/> into it, and then returns the
    /// spawned parcel entity. If insertion fails, the parcel is deleted and <c>null</c> is returned.
    /// </summary>
    /// <param name="toWrap">The entity to insert into the parcel</param>
    /// <param name="parcelProto">The prototype of the parcel to spawn. If null, uses <see cref="DefaultWrappedParcel"/></param>
    /// <param name="parcelMaintainsWrappedSize">
    /// If true, the spawned parcel's size is set to <paramref name="toWrap"/>'s size. If false, or if
    /// <paramref name="toWrap"/> is not an <see cref="ItemComponent">item</see>, the parcel's size is not modified from
    /// whatever is on its prototype.
    /// </param>
    /// <param name="parcelMaintainsWrappedShape">Works the same as <see cref="parcelMaintainsWrappedSize"/>, but for shape.</param>
    public Entity<WrappedParcelComponent>? Wrap(
        EntityUid toWrap,
        EntProtoId<WrappedParcelComponent>? parcelProto = null,
        bool parcelMaintainsWrappedSize = true,
        bool parcelMaintainsWrappedShape = true
    )
    {
        var toWrapXform = Transform(toWrap);
        var spawned = PredictedSpawnAtPosition(parcelProto ?? DefaultWrappedParcel, toWrapXform.Coordinates);
        _transform.SetLocalRotation(spawned, toWrapXform.LocalRotation);

        // If this wrap maintains the size when wrapping, set the parcel's size to the target's size. Otherwise use the
        // wrap's fallback size.
        TryComp(toWrap, out ItemComponent? targetItemComp);
        var size = _fallbackParcelSize;
        if (parcelMaintainsWrappedSize && targetItemComp is not null)
        {
            size = targetItemComp.Size;
        }

        // ParcelWrap's spawned entity should always have an `ItemComp`. As of writing, the only use has it hardcoded on
        // its prototype.
        var item = Comp<ItemComponent>(spawned);
        _item.SetSize(spawned, size, item);
        _appearance.SetData(spawned, WrappedParcelVisuals.Size, size.Id);

        // If this wrap maintains the shape when wrapping and the item has a shape override, copy the shape override to
        // the parcel.
        if (parcelMaintainsWrappedShape && targetItemComp is { Shape: { } shape })
        {
            _item.SetShape(spawned, shape, item);
        }

        // If the target's in a container, try to put the parcel in its place in the container.
        if (_container.TryGetContainingContainer((toWrap, null, null), out var containerOfTarget))
        {
            _container.Remove(toWrap, containerOfTarget);
            _container.InsertOrDrop((spawned, null, null), containerOfTarget);
        }

        var parcel = EnsureComp<WrappedParcelComponent>(spawned);
        if (!IsClientSide(spawned))
        {
            // Insert the target into the parcel.
            // This can only be done on the server as the client-predicted parcel will be deleted, deleting the
            // contained entity as well, desynchronizing the client's from the server's state.
            if (!_container.Insert(toWrap, parcel.Contents))
            {
                DebugTools.Assert(
                    $"Failed to insert target entity into newly spawned parcel. target={PrettyPrint.PrintUserFacing(toWrap)}");
                QueueDel(spawned);
                return null;
            }
        }

        return (spawned, parcel);
    }
}

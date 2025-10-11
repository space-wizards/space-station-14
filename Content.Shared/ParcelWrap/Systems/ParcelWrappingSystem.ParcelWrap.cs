using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.ParcelWrap.Systems;

// This part handles Parcel Wrap.
public sealed partial class ParcelWrappingSystem
{
    private void InitializeParcelWrap()
    {
        SubscribeLocalEvent<ParcelWrapComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ParcelWrapComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbsForParcelWrap);
        SubscribeLocalEvent<ParcelWrapComponent, ParcelWrapItemDoAfterEvent>(OnWrapItemDoAfter);
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
            WrapInternal(args.User, wrapper, target);
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
    private void WrapInternal(EntityUid user, Entity<ParcelWrapComponent> wrapper, EntityUid target)
    {
        if (_net.IsServer)
        {
            var spawned = Spawn(wrapper.Comp.ParcelPrototype, Transform(target).Coordinates);

            // If this wrap maintains the size when wrapping, set the parcel's size to the target's size. Otherwise use the
            // wrap's fallback size.
            TryComp(target, out ItemComponent? targetItemComp);
            var size = wrapper.Comp.FallbackItemSize;
            if (wrapper.Comp.WrappedItemsMaintainSize && targetItemComp is not null)
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
            if (wrapper.Comp.WrappedItemsMaintainShape && targetItemComp is { Shape: { } shape })
            {
                _item.SetShape(spawned, shape, item);
            }

            // If the target's in a container, try to put the parcel in its place in the container.
            if (_container.TryGetContainingContainer((target, null, null), out var containerOfTarget))
            {
                _container.Remove(target, containerOfTarget);
                _container.InsertOrDrop((spawned, null, null), containerOfTarget);
            }

            // Insert the target into the parcel.
            var parcel = EnsureComp<WrappedParcelComponent>(spawned);
            if (!_container.Insert(target, parcel.Contents))
            {
                DebugTools.Assert(
                    $"Failed to insert target entity into newly spawned parcel. target={PrettyPrint.PrintUserFacing(target)}");
                QueueDel(spawned);
            }
        }

        // Consume a `use` on the wrapper, and delete the wrapper if it's empty.
        _charges.TryUseCharges(wrapper.Owner, 1);
        if (_net.IsServer && _charges.IsEmpty(wrapper.Owner))
            QueueDel(wrapper);

        // Play a wrapping sound.
        _audio.PlayPredicted(wrapper.Comp.WrapSound, target, user);
    }
}

using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ParcelWrap.Systems;

// This part handles Parcel Wrap.
public sealed partial class ParcelWrappingSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly INetManager _net = default!;

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
            WrapInternal(args.User, wrapper, target);
            args.Handled = true;
        }
    }

    private bool TryStartWrapDoAfter(EntityUid user, Entity<ParcelWrapComponent> wrapper, EntityUid target)
    {
        var duration = wrapper.Comp.WrapDelay;

        if (TryComp<ParcelWrapOverrideComponent>(target, out var overrideComp) &&
            overrideComp.WrapDelay is { } wrapDelayOverride)
            duration = wrapDelayOverride;

        // In case the target is a player inform them with a popup.
        if (target == user)
        {
            var selfMsg = Loc.GetString("parcel-wrap-popup-being-wrapped-self");
            _popup.PopupClient(selfMsg, user, user);
        }
        else
        {
            var othersMsg = Loc.GetString(
                "parcel-wrap-popup-being-wrapped",
                ("user", Identity.Entity(user, EntityManager))
            );
            _popup.PopupEntity(othersMsg, target, target, PopupType.MediumCaution);
        }

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            duration,
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
        // Consume a `use` on the wrapper, and delete the wrapper if it's empty.
        _charges.TryUseCharges(wrapper.Owner, 1);
        if (_charges.IsEmpty(wrapper.Owner))
            PredictedQueueDel(wrapper);

        // Play a wrapping sound.
        _audio.PlayPredicted(wrapper.Comp.WrapSound, target, user);

        if (_net.IsClient)
            return; // Predicted spawns can't be interacted with yet.

        // Spawn the actual parcel entity.
        var targetTransform = Transform(target);
        var spawned = Spawn(GetParcelPrototype(wrapper, target), targetTransform.Coordinates);
        _transform.SetLocalRotation(spawned, targetTransform.LocalRotation);

        // If the target is in a container, try to put the parcel in its place in the container.
        if (_container.TryGetContainingContainer(target, out var containerOfTarget))
        {
            _container.Remove(target, containerOfTarget);
            _container.InsertOrDrop(spawned, containerOfTarget);
        }

        // Insert the target into the parcel.
        var parcel = EnsureComp<WrappedParcelComponent>(spawned);
        parcel.CanSelfUnwrap = wrapper.Comp.CanSelfUnwrap;
        Dirty(spawned, parcel);

        if (!_container.Insert(target, parcel.Contents))
        {
            DebugTools.Assert(
                $"Failed to insert target entity into newly spawned parcel. target={PrettyPrint.PrintUserFacing(target)}");
            PredictedDel(spawned);
        }
    }

    private EntProtoId<WrappedParcelComponent> GetParcelPrototype(
        Entity<ParcelWrapComponent> wrapper,
        Entity<ParcelWrapOverrideComponent?> target
    )
    {
        // If an override is defined on the target, use that.
        if (TryComp(target, out target.Comp))
            return target.Comp.ParcelPrototype;

        return wrapper.Comp.ParcelPrototype;
    }
}

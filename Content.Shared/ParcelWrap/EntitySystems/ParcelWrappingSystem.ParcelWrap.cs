using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;

namespace Content.Shared.ParcelWrap.EntitySystems;

// This part handles Parcel Wrap.
public abstract partial class SharedParcelWrappingSystem
{
    private void OnExamined(Entity<ParcelWrapComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(
            Loc.GetString("parcel-wrap-examine-detail-uses",
                ("uses", entity.Comp.Uses),
                ("markupUsesColor", "lightgray")
            )
        );
    }

    private void OnAfterInteract(Entity<ParcelWrapComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            args.Target is not { } target ||
            !args.CanReach ||
            !IsWrappable(entity, target))
            return;

        TryStartWrapDoAfter(args.User, entity, target);

        args.Handled = true;
    }

    private void OnGetVerbsForParcelWrap(Entity<ParcelWrapComponent> entity,
        ref GetVerbsEvent<UtilityVerb> args)
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

    private bool TryStartWrapDoAfter(EntityUid user, Entity<ParcelWrapComponent> wrapper, EntityUid target) =>
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
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

[Serializable, NetSerializable]
public sealed partial class ParcelWrapItemDoAfterEvent : SimpleDoAfterEvent;

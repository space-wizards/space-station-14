using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.ParcelWrap.Components;
using Content.Shared.Verbs;

namespace Content.Shared.ParcelWrap.Systems;

// This part handles Parcel Wrap.
public abstract partial class SharedParcelWrappingSystem
{
    private void InitializeParcelWrap()
    {
        SubscribeLocalEvent<ParcelWrapComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ParcelWrapComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ParcelWrapComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbsForParcelWrap);
        SubscribeLocalEvent<ParcelWrapComponent, ParcelWrapItemDoAfterEvent>(OnWrapItemDoAfter);
    }

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
    protected abstract void WrapInternal(EntityUid user, Entity<ParcelWrapComponent> wrapper, EntityUid target);
}

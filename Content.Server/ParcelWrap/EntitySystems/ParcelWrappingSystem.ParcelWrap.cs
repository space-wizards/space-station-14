using Content.Server.ParcelWrap.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;

namespace Content.Server.ParcelWrap.EntitySystems;

// This part handles Parcel Wrap.
public sealed partial class ParcelWrappingSystem
{
    private void OnAfterInteract(Entity<ParcelWrapComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            args.Target is not { } target ||
            !args.CanReach ||
            !IsWrappable(entity, target))
            return;

        WrapInternal(args.User, entity, target);

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
            Act = () => WrapInternal(user, entity, target),
        });
    }
}

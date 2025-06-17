using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Borgs;

public partial class SharedBorgSystem
{
    public virtual void InitializeMMI()
    {
        SubscribeLocalEvent<MMIIncompatibleComponent, ContainerGettingInsertedAttemptEvent>(OnMMIIncompatibleAttemptInsert);
    }

    private void OnMMIIncompatibleAttemptInsert(
        Entity<MMIIncompatibleComponent> entity,
        ref ContainerGettingInsertedAttemptEvent args)
    {
        if (!HasComp<MMIComponent>(args.Container.Owner))
            return;

        args.Cancel();
        Popup.PopupEntity(
            Loc.GetString(entity.Comp.FailureMessage, ("brain", entity), ("mmi", args.Container.Owner)),
            args.Container.Owner);
    }
}

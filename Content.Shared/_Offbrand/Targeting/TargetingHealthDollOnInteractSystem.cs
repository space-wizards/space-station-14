using Content.Shared.Body;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Utility;

namespace Content.Shared._Offbrand.Targeting;

public sealed partial class TargetingHealthDollOnInteractSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TargetingHealthDollOnInteractComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<TargetingHealthDollOnInteractComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target || !HasComp<BodyComponent>(target))
            return;

        _examine.SendExamineTooltip(args.User, target, new FormattedMessage(), false, true, true);
        args.Handled = true;
    }
}

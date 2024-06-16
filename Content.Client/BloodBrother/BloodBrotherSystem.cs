using Content.Shared.Antag;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Traitor.Components;

namespace Content.Client.BloodBrother;

public sealed class BloodBrotherSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedBloodBrotherComponent, CanDisplayStatusIconsEvent>(OnShowIcon);
    }

    private void OnShowIcon<T>(EntityUid uid, T comp, ref CanDisplayStatusIconsEvent args) where T : IAntagStatusIconComponent
    {
        args.Cancelled =
            args.User == null
            || !HasComp<SharedBloodBrotherComponent>(uid)
            || Comp<SharedBloodBrotherComponent>(uid).TeamID != Comp<SharedBloodBrotherComponent>((EntityUid) args.User).TeamID;
    }
}

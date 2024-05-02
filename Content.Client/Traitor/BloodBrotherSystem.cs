using Content.Shared.Antag;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Traitor.Components;

namespace Content.Client.Traitor;

public sealed class BloodBrotherSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherComponent, CanDisplayStatusIconsEvent>(OnShowIcon);
    }

    private void OnShowIcon<T>(EntityUid uid, T comp, ref CanDisplayStatusIconsEvent args) where T : IAntagStatusIconComponent
    {
        args.Cancelled = !HasComp<BloodBrotherComponent>(uid);
    }
}

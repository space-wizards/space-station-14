using Content.Shared.Antag;
using Content.Shared.BloodBrother.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared.BloodBrother;

public sealed class SharedBloodBrotherSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedBloodBrotherComponent, ComponentGetStateAttemptEvent>(OnGetStateAttempt);
    }

    private void OnGetStateAttempt(EntityUid uid, SharedBloodBrotherComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = CanGetState(args.Player, comp);
    }

    private bool CanGetState(ICommonSession? player, SharedBloodBrotherComponent self)
    {
        if (player?.AttachedEntity is not { } uid)
            return true;

        if (TryComp<SharedBloodBrotherComponent>(uid, out var target) && self.TeamID == target.TeamID)
            return true;

        return HasComp<ShowAntagIconsComponent>(uid);
    }
}

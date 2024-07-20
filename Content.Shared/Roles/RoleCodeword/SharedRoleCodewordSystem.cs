using Content.Shared.Mind;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared.Roles.RoleCodeword;

public abstract class SharedRoleCodewordSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleCodewordComponent, ComponentGetStateAttemptEvent>(OnCodewordCompGetStateAttempt);
    }

    /// <summary>
    /// Determines if a HeadRev component should be sent to the client.
    /// </summary>
    private void OnCodewordCompGetStateAttempt(EntityUid uid, RoleCodewordComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player, comp);
    }

    /// <summary>
    /// The criteria that determine whether a codeword component should be sent to a client.
    /// </summary>
    /// <param name="player"> The Player the component will be sent to.</param>
    /// <returns></returns>
    private bool CanGetState(ICommonSession? player, RoleCodewordComponent comp)
    {
        //Apparently this can be null in replays.
        if (player?.AttachedEntity is not { } uid)
            return false;

        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            return false;

        if (!TryComp(mindId, out RoleCodewordComponent? playerComp) && comp != playerComp)
            return false;

        return true;
    }
}

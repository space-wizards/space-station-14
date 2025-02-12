using Content.Shared.Mind;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared._Impstation.LeaderHighlight;

public abstract class SharedLeaderHighlightSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LeaderHighlightComponent, ComponentGetStateAttemptEvent>(OnCompGetStateAttempt);
    }

    /// <summary>
    /// Determines if LeaderHighlightComponent should be sent to the client.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnCompGetStateAttempt(Entity<LeaderHighlightComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player, ent.Comp);
    }

    /// <summary>
    /// Only sends the component if its owner is the player mind. Else returns false.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="comp"></param>
    /// <returns></returns>
    private bool CanGetState(ICommonSession? player, LeaderHighlightComponent comp)
    {
        if (!_mindSystem.TryGetMind(player, out EntityUid mindId, out var _))
            return false;

        if (!TryComp(mindId, out LeaderHighlightComponent? playerComp) && comp != playerComp)
            return false;

        return true;
    }
}

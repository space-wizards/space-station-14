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
    /// Determines if a codeword component should be sent to the client.
    /// </summary>
    private void OnCodewordCompGetStateAttempt(EntityUid uid, RoleCodewordComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player, comp);
    }

    /// <summary>
    /// The criteria that determine whether a codeword component should be sent to a client.
    /// Sends the component if its owner is the player mind.
    /// </summary>
    /// <param name="player"> The Player the component will be sent to.</param>
    /// <param name="comp"> The component being checked against</param>
    /// <returns></returns>
    private bool CanGetState(ICommonSession? player, RoleCodewordComponent comp)
    {
        if (!_mindSystem.TryGetMind(player, out EntityUid mindId, out var _))
            return false;

        if (!TryComp(mindId, out RoleCodewordComponent? playerComp) && comp != playerComp)
            return false;

        return true;
    }

    public void SetRoleCodewords(RoleCodewordComponent comp, string key, List<string> codewords, Color color)
    {
        var data = new CodewordsData(color, codewords);
        comp.RoleCodewords[key] = data;
    }
}

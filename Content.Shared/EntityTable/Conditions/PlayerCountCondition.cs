using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Condition that passes only if the server player count is within a certain range.
/// </summary>
public sealed partial class PlayerCountCondition : EntityTableCondition
{
    /// <summary>
    /// Minimum players of needed for this condition to succeed. Inclusive.
    /// </summary>
    [DataField]
    public int Min = int.MinValue;

    /// <summary>
    /// Maximum numbers of players there can be for this condition to succeed. Inclusive.
    /// </summary>
    [DataField]
    public int Max = int.MaxValue;

    private static ISharedPlayerManager? _playerManager;

    protected override bool EvaluateImplementation(EntityTableSelector root, IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        // Don't resolve this repeatedly
        _playerManager ??= IoCManager.Resolve<ISharedPlayerManager>();

        var playerCount = _playerManager.PlayerCount;

        return playerCount >= Min && playerCount <= Max;
    }
}

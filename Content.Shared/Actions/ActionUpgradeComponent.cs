using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions;

// For actions that can use basic upgrades
// Not all actions should be upgradable
[RegisterComponent, NetworkedComponent, Access(typeof(ActionUpgradeSystem))]
public sealed partial class ActionUpgradeComponent : Component
{
    /// <summary>
    ///     Current Level of the action.
    /// </summary>
    [ViewVariables]
    public int Level = 1;

    /// <summary>
    ///     What level(s) effect this action?
    ///     You can skip levels, so you can have this entity change at level 2 but then won't change again until level 5.
    /// </summary>
    [DataField("effectedLevels"), ViewVariables]
    public Dictionary<int, EntProtoId> EffectedLevels = new();

    // TODO: Branching level upgrades
}

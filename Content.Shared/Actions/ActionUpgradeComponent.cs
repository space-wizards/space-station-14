using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Actions;

// For actions that can use basic upgrades
// Not all actions should be upgradable
[RegisterComponent, NetworkedComponent, Access(typeof(ActionUpgradeSystem))]
public sealed partial class ActionUpgradeComponent : Component
{
    [ViewVariables]
    public string OriginalName = default!;

    /// <summary>
    ///     Current Level of the action, see <see cref="MaxLevel"/> for the maximum level
    /// </summary>
    [ViewVariables]
    public int Level = 1;

    // TODO: Can probably get rid of max level
    // can check the dict for keys?
    /// <summary>
    ///     What is the maximum level the action can achieve, if any?
    ///     There won't be any changes to the ability if there aren't any more parameters to modify
    /// </summary>
    [DataField("maxLevel")]
    public int? MaxLevel = 4;

    // TODO: Change this to go by protos instead
    // TODO: Have Fireball 2 inherit from fireball 1 and only change the params you need
    // TODO: Index list by level or dict
    // Next level and prev level fields
    //  IE Something wants level 2 and level 5 but not level 3 and 4
    // Realistically it'll delete the entity and replace it with a new one.
    // TODO: Replacing action on the slot it was in
    /// <summary>
    ///     What level(s) effect this action?
    ///     You can skip levels, so you can have this entity change at level 2 but then won't change again until level 5.
    /// </summary>
    [DataField("effectedLevels"), ViewVariables]
    public Dictionary<int, EntProtoId> EffectedLevels = new();

    // TODO: Branching level upgrades
}

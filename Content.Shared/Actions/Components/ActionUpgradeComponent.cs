using Content.Shared.Actions;
﻿using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// For actions that can use basic upgrades
/// Not all actions should be upgradable
/// Requires <see cref="ActionComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ActionUpgradeSystem))]
[EntityCategory("Actions")]
public sealed partial class ActionUpgradeComponent : Component
{
    /// <summary>
    ///     Current Level of the action.
    /// </summary>
    [DataField]
    public int Level = 1;

    /// <summary>
    ///     What level(s) effect this action?
    ///     You can skip levels, so you can have this entity change at level 2 but then won't change again until level 5.
    /// </summary>
    [DataField]
    public Dictionary<int, EntProtoId> EffectedLevels = new();

    // TODO: Branching level upgrades
}

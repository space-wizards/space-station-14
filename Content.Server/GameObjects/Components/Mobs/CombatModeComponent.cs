using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    /// <summary>
    ///     Stores whether an entity is in "combat mode"
    ///     This is used to differentiate between regular item interactions or
    ///     using *everything* as a weapon.
    /// </summary>
    public sealed class CombatModeComponent : Component
    {
        public override string Name => "CombatMode";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsInCombatMode { get; set; }
    }
}

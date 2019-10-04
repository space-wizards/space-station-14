using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    /// <summary>
    ///     Stores whether an entity is in "combat mode"
    ///     This is used to differentiate between regular item interactions or
    ///     using *everything* as a weapon.
    /// </summary>
    [RegisterComponent]
    public sealed class CombatModeComponent : SharedCombatModeComponent
    {
        private bool _isInCombatMode;
        private TargetingZone _activeZone;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsInCombatMode
        {
            get => _isInCombatMode;
            set
            {
                _isInCombatMode = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public TargetingZone ActiveZone
        {
            get => _activeZone;
            set
            {
                _activeZone = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new CombatModeComponentState(IsInCombatMode, ActiveZone);
        }
    }
}

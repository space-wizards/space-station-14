using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedCombatModeComponent : Component
    {
        public sealed override uint? NetID => ContentNetIDs.COMBATMODE;
        public override string Name => "CombatMode";
        public sealed override Type StateType => typeof(CombatModeComponentState);

        [Serializable, NetSerializable]
        protected sealed class CombatModeComponentState : ComponentState
        {
            public bool IsInCombatMode { get; }
            public TargetingZone TargetingZone { get; }

            public CombatModeComponentState(bool isInCombatMode, TargetingZone targetingZone)
                : base(ContentNetIDs.COMBATMODE)
            {
                IsInCombatMode = isInCombatMode;
                TargetingZone = targetingZone;
            }
        }
    }
}

using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedCombatModeComponent : Component
    {
        public sealed override uint? NetID => ContentNetIDs.COMBATMODE;
        public override string Name => "CombatMode";

        private bool _isInCombatMode;
        private TargetingZone _activeZone;

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool IsInCombatMode
        {
            get => _isInCombatMode;
            set
            {
                _isInCombatMode = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual TargetingZone ActiveZone
        {
            get => _activeZone;
            set
            {
                _activeZone = value;
                Dirty();
            }
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not CombatModeComponentState state)
                return;

            IsInCombatMode = state.IsInCombatMode;
            ActiveZone = state.TargetingZone;
        }


        public override ComponentState GetComponentState()
        {
            return new CombatModeComponentState(IsInCombatMode, ActiveZone);
        }

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

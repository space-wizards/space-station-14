using System;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedStunnableComponent : Component, IMoveSpeedModifier, IActionBlocker
    {
        public sealed override string Name => "Stunnable";
        public override uint? NetID => ContentNetIDs.STUNNABLE;

        [ViewVariables] protected float WalkModifierOverride = 0f;
        [ViewVariables] protected float RunModifierOverride = 0f;

        [ViewVariables] public abstract bool Stunned { get; }
        [ViewVariables] public abstract bool KnockedDown { get; }
        [ViewVariables] public abstract bool SlowedDown { get; }

        #region ActionBlockers
        public bool CanMove() => (!Stunned);

        public bool CanInteract() => (!Stunned);

        public bool CanUse() => (!Stunned);

        public bool CanThrow() => (!Stunned);

        public bool CanSpeak() => true;

        public bool CanDrop() => (!Stunned);

        public bool CanPickup() => (!Stunned);

        public bool CanEmote() => true;

        public bool CanAttack() => (!Stunned);

        public bool CanEquip() => (!Stunned);

        public bool CanUnequip() => (!Stunned);
        public bool CanChangeDirection() => true;
        #endregion

        [ViewVariables]
        public float WalkSpeedModifier => (SlowedDown ? (WalkModifierOverride <= 0f ? 0.5f : WalkModifierOverride) : 1f);
        [ViewVariables]
        public float SprintSpeedModifier => (SlowedDown ? (RunModifierOverride <= 0f ? 0.5f : RunModifierOverride) : 1f);

        [Serializable, NetSerializable]
        protected sealed class StunnableComponentState : ComponentState
        {
            public bool Stunned { get; }
            public bool KnockedDown { get; }
            public bool SlowedDown { get; }
            public float WalkModifierOverride { get; }
            public float RunModifierOverride { get; }

            public StunnableComponentState(bool stunned, bool knockedDown, bool slowedDown, float walkModifierOverride, float runModifierOverride) : base(ContentNetIDs.STUNNABLE)
            {
                Stunned = stunned;
                KnockedDown = knockedDown;
                SlowedDown = slowedDown;
                WalkModifierOverride = walkModifierOverride;
                RunModifierOverride = runModifierOverride;
            }
        }
    }
}

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Movement.Components;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Timing;
using Robust.Shared.IoC;
using System;
using Content.Shared.Chemistry.Solution;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for stimulants and tranqs. Attempts to find a MovementSpeedModifier on the target,
    /// adding one if not there and to change the movespeed
    /// </summary>
    public class MovespeedModifier : ReagentEffect
    {
        /// <summary>
        /// How much the entities' walk speed is multiplied by.
        /// </summary>
        [DataField("walkSpeedModifier")]
        public float WalkSpeedModifier { get; set; } = 1;

        /// <summary>
        /// How much the entities' run speed is multiplied by.
        /// </summary>
        [DataField("sprintSpeedModifier")]
        public float SprintSpeedModifier { get; set; } = 1;

        /// <summary>
        /// How long the modifier applies (in seconds) when metabolized.
        /// </summary>
        [DataField("statusLifetime")]
        public float StatusLifetime = 2f;

        /// <summary>
        /// Remove reagent at set rate, changes the movespeed modifiers and adds a MovespeedModifierMetabolismComponent if not already there.
        /// </summary>
        public override void Metabolize(IEntity solutionEntity, Solution.ReagentQuantity amount)
        {
            if (!solutionEntity.TryGetComponent(out MovementSpeedModifierComponent? movement)) return;

            solutionEntity.EnsureComponent(out MovespeedModifierMetabolismComponent status);

            // Only refresh movement if we need to.
            var modified = !status.WalkSpeedModifier.Equals(WalkSpeedModifier) ||
                           !status.SprintSpeedModifier.Equals(SprintSpeedModifier);

            status.WalkSpeedModifier = WalkSpeedModifier;
            status.SprintSpeedModifier = SprintSpeedModifier;

            IncreaseTimer(status, StatusLifetime * amount.Quantity.Float());

            if (modified)
                movement.RefreshMovementSpeedModifiers();

        }
        public void IncreaseTimer(MovespeedModifierMetabolismComponent status, float time)
        {
            var gameTiming = IoCManager.Resolve<IGameTiming>();

            var offsetTime = Math.Max(status.ModifierTimer.TotalSeconds, gameTiming.CurTime.TotalSeconds);

            status.ModifierTimer = TimeSpan.FromSeconds(offsetTime + time);
            status.Dirty();
        }
    }
}

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Chemistry.Metabolizable;
using Content.Shared.Movement.Components;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Timing;
using Robust.Shared.IoC;
using System;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for stimulants and tranqs. Attempts to find a MovementSpeedModifier on the target,
    /// adding one if not there and to change the movespeed
    /// </summary>
    [DataDefinition]
    public class MovespeedModifierMetabolism : IMetabolizable
    {
        private IGameTiming _gametiming = IoCManager.Resolve<IGameTiming>();
        /// <summary>
        /// How much of the reagent should be metabolized each sec.
        /// </summary>
        [DataField("rate")]
        public ReagentUnit MetabolismRate { get; set; } = ReagentUnit.New(1);

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
        /// how long the modifier persist after the final unit of reagent is metabolised,
        /// should really be less than however long it takes for a metabolism tick(1 second).
        /// </summary>
        [DataField("statusLifetime")]
        public int StatusLifetime = 3000;

        /// <summary>
        /// Remove reagent at set rate, changes the movespeed modifiers and adds a MovespeedModifierMetabolismComponent if not already there.
        /// </summary>
        /// <param name="solutionEntity"></param>
        /// <param name="reagentId"></param>
        /// <param name="tickTime"></param>
        /// <returns></returns>
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime, ReagentUnit availableReagent)
        {
            if (solutionEntity.TryGetComponent(out MovementSpeedModifierComponent? movement))
            {
                solutionEntity.EnsureComponent(out MovespeedModifierMetabolismComponent status);

                if (status.WalkSpeedModifier != WalkSpeedModifier)
                {
                    status.WalkSpeedModifier = WalkSpeedModifier;
                }
                if (status.SprintSpeedModifier != SprintSpeedModifier)
                {
                    status.SprintSpeedModifier = SprintSpeedModifier;
                }
                if (status.EffectTime != StatusLifetime * MetabolismRate.Int())
                {
                    status.EffectTime = StatusLifetime * MetabolismRate.Int();
                }

                ResetTimer(status);

                //If any of the modifers aren't synced to the movement modifier component, then refresh them, otherwise don't
                //Also I don't know if this is a good way to do a NAND gate in c#
                if (!(status.WalkSpeedModifier.Equals(movement.WalkSpeedModifier) & status.SprintSpeedModifier.Equals(movement.SprintSpeedModifier)))
                    movement?.RefreshMovementSpeedModifiers();

            }
            return MetabolismRate;
        }
        public void ResetTimer(MovespeedModifierMetabolismComponent status)
        {
            status.ModifierTimer = (_gametiming.CurTime, _gametiming.CurTime.Add(TimeSpan.FromMilliseconds(status.EffectTime)));
            status.Dirty();
        }
    }
}

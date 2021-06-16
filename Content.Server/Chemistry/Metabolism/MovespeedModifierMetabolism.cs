using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Server.GameObjects.Components.Chemistry;
using System.Threading;
using Content.Shared.Chemistry.Metabolizable;
using Content.Shared.Movement.Components;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for stimulants and tranqs. Attempts to find a MovementSpeedModifier on the target,
    /// adding one if not there and to change the movespeed
    /// </summary>
    [DataDefinition]
    public class MovespeedModifierMetabolism : IMetabolizable
    {

        /// <summary>
        /// How much of the reagent should be metabolized each sec.
        /// </summary> 
        [DataField("rate")]
        public ReagentUnit MetabolismRate { get; set; } = ReagentUnit.New(1);

        /// <summary>
        /// Walk speed modifier
        /// </summary>
        [DataField("walkSpeedModifier")]
        public float WalkSpeedModifier { get; set; } = 10;

        /// <summary>
        /// Run speed modifier
        /// </summary>
        [DataField("sprintSpeedModifier")]
        public float SprintSpeedModifier { get; set; } = 10;

        [DataField("statusLifetime")]
        public int StatusLifetime = 1200;
        /// <summary>
        /// Remove reagent at set rate, changes the movespeed modifiers and adds a Movespeed
        /// </summary>
        /// <param name="solutionEntity"></param>
        /// <param name="reagentId"></param>
        /// <param name="tickTime"></param>
        /// <returns></returns>
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            if (solutionEntity.TryGetComponent(out MovementSpeedModifierComponent? movement))
            {
                solutionEntity.EnsureComponent(out MovespeedModifierMetabolismComponent status);

                status.WalkSpeedModifier = WalkSpeedModifier;
                status.SprintSpeedModifier = SprintSpeedModifier;
                status.EffectTime = StatusLifetime * MetabolismRate.Int();
                status.ResetTimer();
                movement.RefreshMovementSpeedModifiers();
            }

            return MetabolismRate;
        }

    }
}

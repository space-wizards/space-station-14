using System;
using Content.Server.Explosions;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReactionEffects
{
    [DataDefinition]
    public class ExplosionReactionEffect : IReactionEffect
    {
        [DataField("devastationRange")] private float _devastationRange = 1;
        [DataField("heavyImpactRange")] private float _heavyImpactRange = 2;
        [DataField("lightImpactRange")] private float _lightImpactRange = 3;
        [DataField("flashRange")] private float _flashRange;

        /// <summary>
        /// If true, then scale ranges by intensity. If not, the ranges are the same regardless of reactant amount.
        /// </summary>
        [DataField("scaled")] private bool _scaled;

        /// <summary>
        /// Maximum scaling on ranges. For example, if it equals 5, then it won't scaled anywhere past
        /// 5 times the minimum reactant amount.
        /// </summary>
        [DataField("maxScale")] private float _maxScale = 1;

        public void React(IEntity solutionEntity, double intensity)
        {
            var floatIntensity = (float)intensity;
            if (solutionEntity == null)
                return;
            if(!solutionEntity.TryGetComponent(out SolutionContainerComponent solution))
                return;

            //Handle scaling
            if (_scaled)
            {
                floatIntensity = MathF.Min(floatIntensity, _maxScale);
            }
            else
            {
                floatIntensity = 1;
            }

            //Calculate intensities
            var finalDevastationRange = (int)MathF.Round(_devastationRange * floatIntensity);
            var finalHeavyImpactRange = (int)MathF.Round(_heavyImpactRange * floatIntensity);
            var finalLightImpactRange = (int)MathF.Round(_lightImpactRange * floatIntensity);
            var finalFlashRange = (int)MathF.Round(_flashRange * floatIntensity);
            solutionEntity.SpawnExplosion(finalDevastationRange,
                finalHeavyImpactRange, finalLightImpactRange, finalFlashRange);
        }
    }
}



using System;
using System.Text.Json.Serialization;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReactionEffects
{
    [DataDefinition]
    public sealed class ExplosionReactionEffect : ReagentEffect
    {
        [DataField("devastationRange")]
        [JsonIgnore]
        private float _devastationRange = 1;

        [DataField("heavyImpactRange")]
        [JsonIgnore]
        private float _heavyImpactRange = 2;

        [DataField("lightImpactRange")]
        [JsonIgnore]
        private float _lightImpactRange = 3;

        [DataField("flashRange")]
        [JsonIgnore]
        private float _flashRange;

        /// <summary>
        /// If true, then scale ranges by intensity. If not, the ranges are the same regardless of reactant amount.
        /// </summary>
        [DataField("scaled")]
        [JsonIgnore]
        private bool _scaled;

        /// <summary>
        /// Maximum scaling on ranges. For example, if it equals 5, then it won't scaled anywhere past
        /// 5 times the minimum reactant amount.
        /// </summary>
        [DataField("maxScale")]
        [JsonIgnore]
        private float _maxScale = 1;

        public override bool ShouldLog => true;
        public override LogImpact LogImpact => LogImpact.High;

        public override void Effect(ReagentEffectArgs args)
        {
            var floatIntensity = (float) args.Quantity;

            if (!args.EntityManager.HasComponent<SolutionContainerManagerComponent>(args.SolutionEntity))
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
            EntitySystem.Get<ExplosionSystem>().SpawnExplosion(args.SolutionEntity, finalDevastationRange,
                finalHeavyImpactRange, finalLightImpactRange, finalFlashRange);
        }
    }
}



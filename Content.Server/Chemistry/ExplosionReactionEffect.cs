using System;
using Content.Server.GameObjects.Components.Explosive;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Interfaces;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry
{
    class ExplosionReactionEffect : IReactionEffect
    {
        private float _devastationRange;
        private float _heavyImpactRange;
        private float _lightImpactRange;
        private float _flashRange;

        /// <summary>
        /// If true, then scale ranges by intensity. If not, the ranges are the same regardless of reactant amount.
        /// </summary>
        private bool _scaled;
        /// <summary>
        /// Maximum scaling on ranges. For example, if it equals 5, then it won't scaled anywhere past
        /// 5 times the minimum reactant amount.
        /// </summary>
        private float _maxScale;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _devastationRange, "devastationRange", 1);
            serializer.DataField(ref _heavyImpactRange, "heavyImpactRange", 2);
            serializer.DataField(ref _lightImpactRange, "lightImpactRange", 3);
            serializer.DataField(ref _flashRange, "flashRange", 0);

            serializer.DataField(ref _scaled, "scaled", false);
            serializer.DataField(ref _maxScale, "maxScale", 1);
        }

        public void React(IEntity solutionEntity, int intensity)
        {
            float floatIntensity = intensity; //Use float to avoid truncation in scaling
            if (solutionEntity == null)
                return;
            if(!solutionEntity.TryGetComponent(out SolutionComponent solution))
                return;
            solution.Dispenser?.TryEject();

            //Handle scaling
            var explosive = solutionEntity.AddComponent<ExplosiveComponent>();
            if (_scaled)
            {
                floatIntensity = Math.Min(floatIntensity, _maxScale);
            }
            else
            {
                floatIntensity = 1;
            }

            //Calculate intensities
            explosive.DevastationRange = (int)Math.Round(_devastationRange * floatIntensity);
            explosive.HeavyImpactRange = (int)Math.Round(_heavyImpactRange * floatIntensity);
            explosive.LightImpactRange = (int)Math.Round(_lightImpactRange * floatIntensity);
            explosive.FlashRange = (int)Math.Round(_flashRange * floatIntensity);
            explosive.Explosion();
        }
    }
}

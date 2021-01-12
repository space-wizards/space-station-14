using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.Interfaces.Chemistry;
using Content.Server.Utility;
using Content.Shared.Audio;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.ReactionEffects
{
    [UsedImplicitly]
    public class FoamReactionEffect : IReactionEffect
    {
        /// <summary>
        /// Used for calculating the spread range of the foam based on the intensity of the reaction.
        /// </summary>
        private float _rangeConstant;
        private float _rangeMultiplier;

        private int _maxRange;

        private int _reagentDilutionStart;
        private float _reagentDilutionFactor;
        private float _reagentMaxConcentrationFactor;
        /// <summary>
        /// How many seconds will the foam stay, counting after fully spreading.
        /// </summary>
        private float _duration;
        /// <summary>
        /// How many seconds between each spread step.
        /// </summary>
        private float _spreadDelay;

        private float _removeDelay;

        private string _foamPrototypeId;
        private string _foamSound;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _rangeConstant, "rangeConstant",0f);
            serializer.DataField(ref _rangeMultiplier, "rangeMultiplier",1.1f);
            serializer.DataField(ref _maxRange, "maxRange", 10);
            serializer.DataField(ref _reagentDilutionStart, "reagentDilutionStart", 4);
            serializer.DataField(ref _reagentDilutionFactor, "reagentDilutionFactor",1f);
            serializer.DataField(ref _reagentMaxConcentrationFactor, "reagentMaxConcentrationFactor",2f);
            serializer.DataField(ref _duration, "duration", 10f);
            serializer.DataField(ref _spreadDelay, "spreadDelay", 1f);
            serializer.DataField(ref _removeDelay, "removeDelay", 0f);
            serializer.DataField(ref _foamSound, "foamSound",string.Empty);
            serializer.DataField(ref _foamPrototypeId, "foamPrototypeId", "Foam");
        }

        public void React(IEntity solutionEntity, double intensity)
        {
            if (!solutionEntity.TryGetComponent(out SolutionContainerComponent contents))
                return;

            // We take the square root so it becomes harder to reach higher amount values
            var amount = (int) Math.Round(_rangeConstant + _rangeMultiplier*Math.Sqrt(intensity));
            amount = Math.Min(amount, _maxRange);

            // The maximum value of solutionFraction is _reagentMaxConcentrationFactor, achieved when amount = 0
            // The infimum of solutionFraction is 0, which is approached when amount tends to infinity
            // solutionFraction is equal to 1 only when amount equals _reagentDilutionStart
            float solutionFraction;
            if (amount >= _reagentDilutionStart)
            {
                // Weird formulas here but basically when amount increases, solutionFraction gets closer to 0 in a reciprocal manner
                // _reagentDilutionFactor defines how fast solutionFraction gets closer to 0
                solutionFraction = 1 / (_reagentDilutionFactor*(amount - _reagentDilutionStart) + 1);
            }
            else
            {
                // Here when amount decreases, solutionFraction gets closer to _reagentMaxConcentrationFactor in a linear manner
                solutionFraction = amount * (1 - _reagentMaxConcentrationFactor) / _reagentDilutionStart +
                                   _reagentMaxConcentrationFactor;
            }

            var solution = contents.SplitSolution(contents.MaxVolume);

            solution.RemoveSolution(solution.TotalVolume*(1-solutionFraction));

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryFindGridAt(solutionEntity.Transform.MapPosition, out var grid)) return;

            var coords = grid.MapToGrid(solutionEntity.Transform.MapPosition);
            var ent = solutionEntity.EntityManager.SpawnEntity(_foamPrototypeId, coords.SnapToGrid());

            if (!ent.TryGetComponent(out AreaEffectComponent areaEffectComponent))
            {
                Logger.Error("Couldn't get AreaEffectComponent from " + _foamPrototypeId);
                ent.Delete();
                return;
            }
            if (!ent.TryGetComponent(out FoamComponent foamComponent))
            {
                Logger.Error("Couldn't get FoamComponent from " + _foamPrototypeId);
                ent.Delete();
                return;
            }

            foamComponent.TryAddSolution(solution);
            areaEffectComponent.Start(amount, _duration, _spreadDelay, _removeDelay);

            if (!string.IsNullOrEmpty(_foamSound))
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_foamSound, solutionEntity, AudioHelpers.WithVariation(0.125f));
            }
        }
    }
}

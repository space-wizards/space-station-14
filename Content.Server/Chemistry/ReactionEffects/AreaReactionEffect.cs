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
    public class AreaReactionEffect : IReactionEffect
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <summary>
        /// Used for calculating the spread range of the smoke based on the intensity of the reaction.
        /// </summary>
        private float _rangeConstant;
        private float _rangeMultiplier;

        private int _maxRange;

        private bool _diluteReagents;
        private int _reagentDilutionStart;
        private float _reagentDilutionFactor;
        private float _reagentMaxConcentrationFactor;
        /// <summary>
        /// How many seconds will the smoke stay, counting after fully spreading.
        /// </summary>
        private float _duration;
        /// <summary>
        /// How many seconds between each spread step.
        /// </summary>
        private float _spreadDelay;

        private float _removeDelay;

        private string _prototypeId;
        private string _sound;

        public AreaReactionEffect()
        {
            IoCManager.InjectDependencies(this);
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _rangeConstant, "rangeConstant",0f);
            serializer.DataField(ref _rangeMultiplier, "rangeMultiplier",1.1f);
            serializer.DataField(ref _maxRange, "maxRange", 10);
            serializer.DataField(ref _diluteReagents, "diluteReagents", false);
            serializer.DataField(ref _reagentDilutionStart, "reagentDilutionStart", 4);
            serializer.DataField(ref _reagentDilutionFactor, "reagentDilutionFactor", 1f);
            serializer.DataField(ref _reagentMaxConcentrationFactor, "reagentMaxConcentrationFactor",2f);
            serializer.DataField(ref _duration, "duration", 10f);
            serializer.DataField(ref _spreadDelay, "spreadDelay", 0.5f);
            serializer.DataField(ref _removeDelay, "removeDelay", 0.5f);
            serializer.DataField(ref _sound, "sound", null);
            serializer.DataField(ref _prototypeId, "prototypeId", null);
        }

        public void React(IEntity solutionEntity, double intensity)
        {
            if (!solutionEntity.TryGetComponent(out SolutionContainerComponent contents))
                return;

            var solution = contents.SplitSolution(contents.MaxVolume);
            // We take the square root so it becomes harder to reach higher amount values
            var amount = (int) Math.Round(_rangeConstant + _rangeMultiplier*Math.Sqrt(intensity));
            amount = Math.Min(amount, _maxRange);

            if (_diluteReagents)
            {
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
                solution.RemoveSolution(solution.TotalVolume*(1-solutionFraction));
            }

            if (!_mapManager.TryFindGridAt(solutionEntity.Transform.MapPosition, out var grid)) return;

            var coords = grid.MapToGrid(solutionEntity.Transform.MapPosition);
            var ent = solutionEntity.EntityManager.SpawnEntity(_prototypeId, coords.SnapToGrid());

            if (!ent.TryGetComponent(out AreaEffectComponent areaEffectComponent))
            {
                Logger.Error("Couldn't get AreaEffectComponent from " + _prototypeId);
                ent.Delete();
                return;
            }
            if (ent.TryGetComponent(out SmokeComponent smokeComponent))
            {
                smokeComponent.TryAddSolution(solution);
            }
            else if (ent.TryGetComponent(out FoamComponent foamComponent))
            {
                foamComponent.TryAddSolution(solution);
            }
            else
            {
                Logger.Error("Couldn't get FoamComponent or SmokeComponent from " + _prototypeId);
            }

            areaEffectComponent.Start(amount, _duration, _spreadDelay, _removeDelay);

            if (!string.IsNullOrEmpty(_sound))
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_sound, solutionEntity, AudioHelpers.WithVariation(0.125f));
            }
        }
    }
}

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
    public class SmokeReactionEffect : IReactionEffect
    {
        /// <summary>
        /// Used for calculating the spread range of the smoke based on the intensity of the reaction.
        /// </summary>
        private float _rangeConstant;
        private float _rangeMultiplier;

        private int _maxRange;
        /// <summary>
        /// How many seconds will the smoke stay, counting after fully spreading.
        /// </summary>
        private float _duration;
        /// <summary>
        /// How many seconds between each spread step.
        /// </summary>
        private float _spreadDelay;

        private float _removeDelay;

        private string _smokePrototypeId;
        private string _smokeSound;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _rangeConstant, "rangeConstant",0f);
            serializer.DataField(ref _rangeMultiplier, "rangeMultiplier",1.1f);
            serializer.DataField(ref _maxRange, "maxRange", 10);
            serializer.DataField(ref _duration, "duration", 10f);
            serializer.DataField(ref _spreadDelay, "spreadDelay", 0.5f);
            serializer.DataField(ref _removeDelay, "removeDelay", 0.5f);
            serializer.DataField(ref _smokeSound, "smokeSound",string.Empty);
            serializer.DataField(ref _smokePrototypeId, "smokePrototypeId", "Smoke");
        }

        public void React(IEntity solutionEntity, double intensity)
        {
            if (!solutionEntity.TryGetComponent(out SolutionContainerComponent contents))
                return;

            var solution = contents.SplitSolution(contents.MaxVolume);
            // We take the square root so it becomes harder to reach higher amount values
            var amount = (int) Math.Round(_rangeConstant + _rangeMultiplier*Math.Sqrt(intensity));
            amount = Math.Min(amount, _maxRange);

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryFindGridAt(solutionEntity.Transform.MapPosition, out var grid)) return;

            var coords = grid.MapToGrid(solutionEntity.Transform.MapPosition);
            var ent = solutionEntity.EntityManager.SpawnEntity(_smokePrototypeId, coords.SnapToGrid());

            if (!ent.TryGetComponent(out AreaEffectComponent areaEffectComponent))
            {
                Logger.Error("Couldn't get AreaEffectComponent from " + _smokePrototypeId);
                ent.Delete();
                return;
            }
            if (!ent.TryGetComponent(out SmokeComponent smokeComponent))
            {
                Logger.Error("Couldn't get SmokeComponent from " + _smokePrototypeId);
                ent.Delete();
                return;
            }

            smokeComponent.TryAddSolution(solution);
            areaEffectComponent.Start(amount, _duration, _spreadDelay, _removeDelay);
            if (!string.IsNullOrEmpty(_smokeSound))
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_smokeSound, solutionEntity, AudioHelpers.WithVariation(0.125f));
            }
        }
    }
}

using System;
using Content.Server.Explosions;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.Interfaces.Chemistry;
using Content.Server.Utility;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.ReactionEffects
{
    public class SmokeReactionEffect : IReactionEffect
    {
        /// <summary>
        /// Used for calculating the spread range of the smoke based on the intensity of the reaction.
        /// </summary>
        private float _rangeMultiplier;
        /// <summary>
        /// How many seconds will the smoke stay, counting after fully spreading.
        /// </summary>
        private float _duration;
        /// <summary>
        /// How many seconds between each spread step.
        /// </summary>
        private float _spreadDelay;
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _rangeMultiplier, "rangeMultiplier",0.2f);
            serializer.DataField(ref _duration, "duration", 10f);
            serializer.DataField(ref _spreadDelay, "spreadDelay", 0.5f);
        }

        public void React(IEntity solutionEntity, double intensity)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryFindGridAt(solutionEntity.Transform.MapPosition, out var grid)) return;

            var coords = grid.MapToGrid(solutionEntity.Transform.MapPosition);
            var ent = solutionEntity.EntityManager.SpawnEntity("smoke_cloud", coords.SnapToGrid());

            var smokeComponent = ent.GetComponent<SmokeComponent>();

            var contents = solutionEntity.GetComponent<SolutionContainerComponent>();
            var solution = contents.SplitSolution(contents.MaxVolume);

            smokeComponent.TryAddSolution(solution);

            var amount = (int) Math.Round(intensity * _rangeMultiplier);
            smokeComponent.Start(ent, amount,_duration+2*amount*_spreadDelay,_spreadDelay);
        }
    }
}

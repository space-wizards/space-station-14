using System;
using Content.Server.Explosions;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.Interfaces.Chemistry;
using Content.Server.Utility;
using Content.Shared.Audio;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
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

        private string _smokeSound;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _rangeMultiplier, "rangeMultiplier",0.2f);
            serializer.DataField(ref _duration, "duration", 10f);
            serializer.DataField(ref _spreadDelay, "spreadDelay", 0.5f);
            serializer.DataField(ref _smokeSound, "smokeSound",string.Empty);
        }

        public void React(IEntity solutionEntity, double intensity)
        {
            var contents = solutionEntity.GetComponent<SolutionContainerComponent>();

            var solution = contents.SplitSolution(contents.MaxVolume);
            var amount = (int) Math.Round(intensity * _rangeMultiplier);

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryFindGridAt(solutionEntity.Transform.MapPosition, out var grid)) return;

            var coords = grid.MapToGrid(solutionEntity.Transform.MapPosition);
            var ent = solutionEntity.EntityManager.SpawnEntity("smoke_cloud", coords.SnapToGrid());

            var smokeComponent = ent.GetComponent<SmokeComponent>();
            smokeComponent.Activate(solution, amount, _duration, _spreadDelay);

            EntitySystem.Get<AudioSystem>().PlayFromEntity(_smokeSound, solutionEntity, AudioHelpers.WithVariation(0.125f));
        }
    }
}

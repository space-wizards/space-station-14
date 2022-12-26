using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.ReactionEffects
{
    /// <summary>
    /// Basically smoke and foam reactions.
    /// </summary>
    [UsedImplicitly]
    [ImplicitDataDefinitionForInheritors]
    public abstract class AreaReactionEffect : ReagentEffect, ISerializationHooks
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <summary>
        /// Used for calculating the spread range of the effect based on the intensity of the reaction.
        /// </summary>
        [DataField("rangeConstant")] private float _rangeConstant;
        [DataField("rangeMultiplier")] private float _rangeMultiplier = 1.1f;
        [DataField("maxRange")] private int _maxRange = 10;

        /// <summary>
        /// If true the reagents get diluted or concentrated depending on the range of the effect
        /// </summary>
        [DataField("diluteReagents")] private bool _diluteReagents;

        /// <summary>
        /// Used to calculate dilution. Increasing this makes the reagents more diluted.
        /// </summary>
        [DataField("reagentDilutionFactor")] private float _reagentDilutionFactor = 1f;

        /// <summary>
        /// How many seconds will the effect stay, counting after fully spreading.
        /// </summary>
        [DataField("duration")] private float _duration = 10;

        /// <summary>
        /// How many seconds between each spread step.
        /// </summary>
        [DataField("spreadDelay")] private float _spreadDelay = 0.5f;

        /// <summary>
        /// How many seconds between each remove step.
        /// </summary>
        [DataField("removeDelay")] private float _removeDelay = 0.5f;

        /// <summary>
        /// The entity prototype that will be spawned as the effect. It needs a component derived from SolutionAreaEffectComponent.
        /// </summary>
        [DataField("prototypeId", required: true)]
        private string _prototypeId = default!;

        /// <summary>
        /// Sound that will get played when this reaction effect occurs.
        /// </summary>
        [DataField("sound", required: true)] private SoundSpecifier _sound = default!;

        public override bool ShouldLog => true;
        public override LogImpact LogImpact => LogImpact.High;

        void ISerializationHooks.AfterDeserialization()
        {
            IoCManager.InjectDependencies(this);
        }

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Source == null)
                return;

            var splitSolution = EntitySystem.Get<SolutionContainerSystem>().SplitSolution(args.SolutionEntity, args.Source, args.Source.Volume);
            // We take the square root so it becomes harder to reach higher amount values
            var amount = (int) Math.Round(_rangeConstant + _rangeMultiplier*Math.Sqrt(args.Quantity.Float()));
            amount = Math.Min(amount, _maxRange);

            if (_diluteReagents)
            {
                // The maximum value of solutionFraction is _reagentMaxConcentrationFactor, achieved when amount = 0
                // The infimum of solutionFraction is 0, which is approached when amount tends to infinity
                // solutionFraction is equal to 1 only when amount equals _reagentDilutionStart
                // Weird formulas here but basically when amount increases, solutionFraction gets closer to 0 in a reciprocal manner
                // _reagentDilutionFactor defines how fast solutionFraction gets closer to 0
                float solutionFraction = 1 / (_reagentDilutionFactor*(amount) + 1);
                splitSolution.RemoveSolution(splitSolution.Volume * (1 - solutionFraction));
            }

            var transform = args.EntityManager.GetComponent<TransformComponent>(args.SolutionEntity);

            if (!_mapManager.TryFindGridAt(transform.MapPosition, out var grid)) return;

            var coords = grid.MapToGrid(transform.MapPosition);

            var ent = args.EntityManager.SpawnEntity(_prototypeId, coords.SnapToGrid());

            var areaEffectComponent = GetAreaEffectComponent(ent);

            if (areaEffectComponent == null)
            {
                Logger.Error("Couldn't get AreaEffectComponent from " + _prototypeId);
                IoCManager.Resolve<IEntityManager>().QueueDeleteEntity(ent);
                return;
            }

            areaEffectComponent.TryAddSolution(splitSolution);
            areaEffectComponent.Start(amount, _duration, _spreadDelay, _removeDelay);

            SoundSystem.Play(_sound.GetSound(), Filter.Pvs(args.SolutionEntity), args.SolutionEntity, AudioHelpers.WithVariation(0.125f));
        }

        protected abstract SolutionAreaEffectComponent? GetAreaEffectComponent(EntityUid entity);
    }
}

using Content.Server.Fluids.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReactionEffects
{
    /// <summary>
    /// Basically smoke and foam reactions.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class AreaReactionEffect : ReagentEffect
    {
        /// <summary>
        /// How many seconds will the effect stay, counting after fully spreading.
        /// </summary>
        [DataField("duration")] private float _duration = 10;

        /// <summary>
        /// How many units of reaction for 1 smoke entity.
        /// </summary>
        [DataField] public FixedPoint2 OverflowThreshold = FixedPoint2.New(2.5);

        /// <summary>
        /// The entity prototype that will be spawned as the effect.
        /// </summary>
        [DataField("prototypeId", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string _prototypeId = default!;

        /// <summary>
        /// Sound that will get played when this reaction effect occurs.
        /// </summary>
        [DataField("sound", required: true)] private SoundSpecifier _sound = default!;

        public override bool ShouldLog => true;

        protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-missing");

        public override LogImpact LogImpact => LogImpact.High;

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Source == null)
                return;

            var spreadAmount = (int) Math.Max(0, Math.Ceiling((args.Quantity / OverflowThreshold).Float()));
            var splitSolution = args.Source.SplitSolution(args.Source.Volume);
            var transform = args.EntityManager.GetComponent<TransformComponent>(args.SolutionEntity);
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryFindGridAt(transform.MapPosition, out _, out var grid) ||
                !grid.TryGetTileRef(transform.Coordinates, out var tileRef) ||
                tileRef.Tile.IsSpace())
            {
                return;
            }

            var coords = grid.MapToGrid(transform.MapPosition);
            var ent = args.EntityManager.SpawnEntity(_prototypeId, coords.SnapToGrid());

            var smoke = args.EntityManager.System<SmokeSystem>();
            smoke.StartSmoke(ent, splitSolution, _duration, spreadAmount);

            var audio = args.EntityManager.System<SharedAudioSystem>();
            audio.PlayPvs(_sound, args.SolutionEntity, AudioHelpers.WithVariation(0.125f));
        }
    }
}

using Content.Server.Fluids.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Basically smoke and foam reactions.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class AreaReactionEffect : EntityEffect
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
            => Loc.GetString("reagent-effect-guidebook-area-reaction",
                    ("duration", _duration)
                );

    public override LogImpact LogImpact => LogImpact.High;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            if (reagentArgs.Source == null)
                return;

            var spreadAmount = (int) Math.Max(0, Math.Ceiling((reagentArgs.Quantity / OverflowThreshold).Float()));
            var splitSolution = reagentArgs.Source.SplitSolution(reagentArgs.Source.Volume);
            var transform = reagentArgs.EntityManager.GetComponent<TransformComponent>(reagentArgs.TargetEntity);
            var mapManager = IoCManager.Resolve<IMapManager>();
            var mapSys = reagentArgs.EntityManager.System<MapSystem>();
            var sys = reagentArgs.EntityManager.System<TransformSystem>();
            var mapCoords = sys.GetMapCoordinates(reagentArgs.TargetEntity, xform: transform);

            if (!mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid) ||
                !mapSys.TryGetTileRef(gridUid, grid, transform.Coordinates, out var tileRef) ||
                tileRef.Tile.IsSpace())
            {
                return;
            }

            var coords = mapSys.MapToGrid(gridUid, mapCoords);
            var ent = reagentArgs.EntityManager.SpawnEntity(_prototypeId, coords.SnapToGrid());

            var smoke = reagentArgs.EntityManager.System<SmokeSystem>();
            smoke.StartSmoke(ent, splitSolution, _duration, spreadAmount);

            var audio = reagentArgs.EntityManager.System<SharedAudioSystem>();
            audio.PlayPvs(_sound, reagentArgs.TargetEntity, AudioHelpers.WithVariation(0.125f));
            return;
        }

        // TODO: Someone needs to figure out how to do this for non-reagent effects.
        throw new NotImplementedException();
    }
}

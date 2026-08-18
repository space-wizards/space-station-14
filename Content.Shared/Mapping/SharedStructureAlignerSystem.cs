using Content.Shared.CCVar;
using Content.Shared.Construction.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;

namespace Content.Shared.Mapping;

/// <summary>
/// This system can fix the rotation of structures like doors and firelocks, based on the surrounding walls/windows.
/// See StructureAlignerComponent and StructureAlignToComponent.
/// </summary>
/// <remarks>
/// This only works for sprites that are symmetrical, so only need to worry about 2 rotation states.
/// The correct rotation for sprites that (effectively) have 4 states is too fuzzy to be determined via scripted logic,
/// and they are probably always placed deliberately anyway. So they are exempt from this system.
/// This is primarily an assistive tool for fixing maps that were made before door sprites were directional.
/// </remarks>>
/// TODO But it could also be upgraded to help auto-align construction ghosts for doors, as a potential QOL feature
public sealed partial class SharedStructureAlignerSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedTransformSystem _trans = default!;

    /// <summary>
    /// If enabled, every StructureAlignerComponent will be Aligned when it spawns.
    /// </summary>
    private static bool _mapInitAlign;

    private const float ProximityMin = 0.45f;
    private const float ProximityMax = 1.1f;

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCVars.MapInitAlign,  (b) => { _mapInitAlign = b; }, true);
    }

    [SubscribeLocalEvent]
    private void OnAnchored(Entity<StructureAlignerComponent> entity, ref UserAnchoredEvent args)
    {
        if (!entity.Comp.AnchorAlign)
            return;

        Align(entity);
    }

    /// <summary>
    /// If the cvar is enabled, every StructureAlignerComponent will be Aligned when the map initializes.
    /// </summary>
    /// <remark>May be considered a stopgap measure when unupgraded maps are in rotation?</remark>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<StructureAlignerComponent> entity, ref MapInitEvent args)
    {
        if (!_mapInitAlign)
            return;

        Align(entity);
    }

    /// <summary>
    /// Aligns every StructureAlignerComponent on the target map, or on every map if not specified.
    /// </summary>
    /// <returns> The feedback to be displayed to the user, if the command was triggered from the console</returns>>
    /// <remarks>This can be expensive and lag the game for seconds. It should not be called after players are in the game.</remarks>
    public string AlignAll(MapId? map = null, bool dryRun = false)
    {
        // It needs to be an All Entity Query so it works on pre-init maps during Mapping
        var query = AllEntityQuery<StructureAlignerComponent, TransformComponent>();

        var countAll = 0;
        var countFixed = 0;

        foreach (var (ent, comp, trans) in query)
        {
            if (map is not null && trans.MapID != map)
                continue;

            if (Align((ent, comp), dryRun))
                countFixed++;
            countAll++;
        }

        string message;
        if (countAll == 0)
            message = Loc.GetString("cmd-align-feedback-none", ("dry", dryRun));
        else if (countFixed == 0)
            message = Loc.GetString("cmd-align-feedback-good", ("dry", dryRun));
        else
            message = Loc.GetString("cmd-align-feedback", ("fixed", countFixed), ("dry", dryRun));

        // Logging both alignable and misaligned entities, console only gets the latter
        Log.Info($"AlignAll found {countAll} entities. {countFixed} were misaligned { (dryRun ? ". Dry run, no rotations were performed." : " and has been fixed.") }");
        return message;
    }

    /// <summary>
    /// Aligns the target entity to it's neighboring StructureAlignToComponent-s with matching types.
    /// </summary>
    private bool Align(Entity<StructureAlignerComponent> entity, bool dryRun = false)
    {
        var trans = Transform(entity);

        // Do not align to loose debris
        if (!trans.Anchored)
            return false;

        // Locate adjacent walls and doors
        var query = AllEntityQuery<StructureAlignToComponent, TransformComponent>();

        var northSouth = 0d;
        var eastWest = 0d;

        int weight;
        foreach (var (neighborEnt, neighborComp, neighborTrans) in query)
        {
            if (entity.Owner == neighborEnt)
                continue;

            if (!neighborComp.AlignType.Contains(entity.Comp.AlignType))
                continue;

            // They must be anchored to the same parent to matter
            if (neighborTrans.ParentUid != trans.ParentUid)
                continue;

            // Anchored objects have enough weight in the calculation to make unanchored ones irrelevant,
            // but if only unanchored ones are present, they will still matter.
            // For example, if a line of firelock frames are being anchored, with no adjacent walls
            weight = neighborTrans.Anchored ? 10 : 1;

            // Only the four adjacent tiles should be considered for alignment, otherwise calculation quickly becomes infeasibly complex
            // Minimum range is here to ignore overlapping entities
            trans.Coordinates.TryDistance(EntityManager, _trans, neighborTrans.Coordinates, out var dist);
            if (dist > ProximityMax || dist < ProximityMin)
                continue;

            var vect = trans.Coordinates.Position - neighborTrans.Coordinates.Position;

            eastWest += Math.Abs(Math.Round(vect.X)) * weight;
            northSouth += Math.Round(Math.Abs(vect.Y)) * weight;
        }

        // Determine correct orientation
        // A horizontal base sprite is assumed, with neighbors to the East or West being acceptable.
        // If the entity instead has N or S side neighbors, it will be rotated 90 degrees.
        // If ambiguous (neighbors on all sides, or in an L shape) then no rotation will occur.
        Angle? targetAngle;

        if (eastWest > northSouth)
        {
            targetAngle = Angle.FromDegrees(0);
        }
        else if (northSouth > eastWest)
        {
            targetAngle = Angle.FromDegrees(90);
        }
        else
            return false;

        var locRot = Math.Abs(trans.LocalRotation);
        // Don't want to "fix" a 180 degree misalignment

        // rotate sprite
        if (!MathHelper.CloseTo(locRot, targetAngle.Value, 0.01f)
            && !MathHelper.CloseTo(locRot, targetAngle.Value + Angle.FromDegrees(180), 0.01f))
        {
            var name = MetaData(entity.Owner).EntityName;
            var pos = _trans.GetWorldPosition(trans);

            if (!dryRun)
                _trans.SetLocalRotation(entity, trans.LocalRotation + Angle.FromDegrees(90));

            Log.Info($"Misaligned entity '{ entity.Owner }' on map { trans.MapID } at { pos } : { name }");
            return true;
        }

        return false;
    }
}

public enum StructureAlignType : byte
{
    /// <summary>
    /// Airlocks, doors, shutters, blast doors and everything that would be functionally
    /// considered a room boundary (doors, walls, windows, full tile rocks etc.)
    /// No firelocks - they are their own category to avoid interference in atypical placement locations
    /// No thin walls/doors, or docking airlocks - directionality is too important for these and must be decided manually
    /// </summary>
    Door,
    /// <summary>
    /// Firelocks and everything that would be functionally considered a room boundary
    /// (doors, walls, windows, full tile rocks etc.)
    /// </summary>
    Firelock,
    DoNotAlign,
}

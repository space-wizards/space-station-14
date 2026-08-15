using Content.Shared.CCVar;
using Content.Shared.Construction.Components;
using Robust.Shared;
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
    [Dependency] private IEntityManager _entMan = default!;
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

        Align(entity.AsNullable());
    }

    /// <summary>
    /// If the cvar is enabled, every StructureAlignerComponent will be Aligned when it spawns.
    /// </summary>
    /// <remark>May be considered a stopgap measure when unupgraded maps are in rotation?</remark>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<StructureAlignerComponent> entity, ref MapInitEvent args)
    {
        if (!_mapInitAlign)
            return;

        Align(entity.AsNullable());
    }

    /// <summary>
    /// Aligns every StructureAlignerComponent on the target map, or on every map if not specified.
    /// </summary>
    /// <remarks>This is very expensive and will likely lag the game for seconds. It should never be ran once players are in the game.</remarks>
    public string? AlignAll(MapId? map = null)
    {
        // It needs to be an All Entity Query so it works on pre-init maps during Mapping
        var query = _entMan.AllEntityQueryEnumerator<StructureAlignerComponent, TransformComponent>();

        var countAll = 0;
        var countFixed = 0;

        foreach (var (ent, comp, trans) in query)
        {
            if (map is not null && trans.MapID != map)
                continue;

            if (Align((ent, comp)))
                countFixed++;
            countAll++;
        }

        return ($"Found {countAll} alignable entities, of which {countFixed} were rotated.");  // TODO:ERRANT localize? Make this a log instead of a return?
    }

    /// <summary>
    /// Aligns the target entity to it's neighboring StructureAlignToComponent-s with matching types.
    /// </summary>
    private bool Align(Entity<StructureAlignerComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var trans = Transform(entity);

        // Do not align to loose debris
        if (!trans.Anchored)
            return false;

        // Locate adjacent walls and doors
        var query = _entMan.AllEntityQueryEnumerator<StructureAlignToComponent>();

        var northSouth = 0d;
        var eastWest = 0d;
        var neighbors = 0;

        // if (!HasComp<NavMapDoorComponent>(entity)) //TODO:ERRANT testing only
        // {
        //     var a = entity.Owner;
        // }

        foreach (var (ent, comp) in query)
        {
            if (entity.Owner == ent)
                continue;

            if (!comp.AlignType.Contains(entity.Comp.AlignType))
                continue;

            var t = Transform(ent);

            // They must be anchored to the same parent to matter
            if (t.ParentUid != trans.ParentUid)
                continue;

            if (!t.Anchored) //TODO:ERRANT governed via cvar? unanchored frames could have a LITTLE weight
                continue;

            // Only the four adjacent tiles should be considered for alignment, otherwise calculation quickly becomes infeasibly complex
            // Minimum range is here to ignore overlapping entities
            trans.Coordinates.TryDistance(EntityManager, t.Coordinates, out var dist);
            if (dist > ProximityMax || dist < ProximityMin)
                continue;

            // var inMinRange = _trans.InRange(t.Coordinates, trans.Coordinates, ProximityMax); //TODO:ERRANT this one is broken
            // var inMaxRange = _trans.InRange(t.Coordinates, trans.Coordinates, ProximityMin);
            // if(!(inMaxRange && !inMinRange))
            //     continue;

            neighbors++;

            var vect = trans.Coordinates.Position - t.Coordinates.Position;

            eastWest += Math.Abs(Math.Round(vect.X));
            northSouth += Math.Round(Math.Abs(vect.Y));
        }

        // Do we care about neighbor count?

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
        // Maybe airlocks should only have 2 rot states in the first place?

        // rotate sprite
        if (!MathHelper.CloseTo(locRot, targetAngle.Value, 0.01f)
            && !MathHelper.CloseTo(locRot, targetAngle.Value + Angle.FromDegrees(180), 0.01f))
        {
            var meta = MetaData(entity.Owner);


            _trans.SetLocalRotation(entity, trans.LocalRotation + Angle.FromDegrees(90));

            Log.Info($"Aligned entity '{entity.Owner }' on map {trans.MapID} at {trans.WorldPosition.Ceiled()} : { meta.EntityName}"); //TODO:ERRANT Loglevel to debug? //Obsolete!
            return true;
        }

        return false;
    }
}

public enum StructureAlignType : byte
{
    /// <summary>
    /// Airlocks, doors, shutters, blast doors and everything that would be functionally
    /// considered a room boundary (doors, walls, windows, full tile rock)
    /// No firelocks
    /// No thin walls/doors //TODO:ERRANT should turnstiles be in either?
    /// </summary>
    Door,
    /// <summary>
    /// Firelocks and everything that would be functionally considered a room boundary
    /// (doors, walls, windows, full tile rock)
    /// </summary>
    Firelock,
    DoNotAlign, //TODO:ERRANT this should be removed and fixed by not inheriting
}

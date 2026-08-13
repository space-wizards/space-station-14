using Content.Shared.Examine;
using Content.Shared.Pinpointer;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;

namespace Content.Shared.Mapping;

/// <summary>
///
/// </summary>
/// <remarks>
/// What this system does is always at least a little expensive, calculation-wise.
/// Sometimes it is "several seconds" expensive. Use it carefully.
/// </remarks>>
public sealed partial class SharedSatanSystem : EntitySystem // StructureAlignmentTAgNonsmoothing
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private SharedTransformSystem _trans = default!;

    private static bool _mint; //TODO:ERRANT rename

    public override void Initialize()
    {
        // _cfg.OnValueChanged(CVars.SatanOnMapInit, _mint, true);
    }


    //TODO:ERRANT For testing only
    [SubscribeLocalEvent]
    private void OnExamined(Entity<SatanAlignComponent> entity, ref ExaminedEvent args)
    {

            Align(entity.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SatanAlignComponent> entity, ref MapInitEvent args)
    {
        if (_mint)
            Align(entity.AsNullable());
    }

    /// <summary>
    /// Runs align on every alignable entity.
    /// This is very expensive and should never be ran after players are in the game!
    /// </summary>
    public string? AlignAll(MapId? map = null)
    {
        // Needs to be an All Entity Query so it works on pre-init maps
        var query = _entMan.AllEntityQueryEnumerator<SatanAlignComponent, TransformComponent>();

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

        return ($"Found {countAll} alignable entities, of which {countFixed} were rotated.");  // TODO:ERRANT localize?
    }

    /// <summary>
    /// Makes the target entity align to adjacent compatible entities
    /// </summary>
    /// <remarks> Only works for entities which are symmetrical, so north-south and east-west headings are interchangeable</remarks>
    private bool Align(Entity<SatanAlignComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var trans = Transform(entity); //TODO:ERRANT get this from the calling function

        if (!trans.Anchored)
            return false;

        if (!TryComp<SatanKindComponent>(entity, out var kind))
            return false;

        // locate nearby walls and doors
        var query = _entMan.AllEntityQueryEnumerator<SatanKindComponent>();

        var ns = 0d;
        var ew = 0d;
        var neighbors = 0;

        if (!HasComp<NavMapDoorComponent>(entity)) //TODO:ERRANT testing only
        {
            var a = entity.Owner;
        }

        foreach (var (ent, comp) in query)
        {
            if (entity.Owner == ent)
                continue;

            if (kind.AlignType != comp.AlignType)
                continue;

            var t = Transform(ent);

            // They must be anchored to the same parent to matter
            if (t.ParentUid != trans.ParentUid)
                continue;

            if (!t.Anchored)
                continue;

            trans.Coordinates.TryDistance(EntityManager, t.Coordinates, out var dist); //TODO:ERRANT use _trans.InRange ?
            if (dist > entity.Comp.ProximityMax || dist < entity.Comp.ProximityMin)
                continue;

            var inMinRange = _trans.InRange(t.Coordinates, trans.Coordinates, entity.Comp.ProximityMax);
            var inMaxRange = _trans.InRange(t.Coordinates, trans.Coordinates, entity.Comp.ProximityMin);
            // if(!(inMaxRange && !inMinRange))
            //     continue;

            neighbors++;

            var vect = trans.Coordinates.Position - t.Coordinates.Position;

            ew += Math.Abs(Math.Round(vect.X));
            ns += Math.Round(Math.Abs(vect.Y));
        }

        // Do we care about neighbor count?

        // determine correct orientation
        var targetAngle = Angle.FromDegrees(0);
        if (ns > ew)
        {
            targetAngle = Angle.FromDegrees(90);
        }

        if (ew == ns) //TODO:ERRANT fix this shit
        {
            // Log.Warning($"Airlock {entity} was not rotated");
        }


        var locRot = Math.Abs(trans.LocalRotation);
        // Don't want to "fix" a 180 degree misalignment
        // Maybe airlocks should only have 2 rot states in the first place?

        // rotate sprite
        if (!MathHelper.CloseTo(locRot, targetAngle, 0.01f)
            && !MathHelper.CloseTo(locRot, targetAngle + Angle.FromDegrees(180), 0.01f))
        {
            _trans.SetLocalRotation(entity, trans.LocalRotation + Angle.FromDegrees(90));

            Log.Debug($"Satan aligned entity: {entity.Owner}"); //TODO:ERRANT testing only
            return true;
        }

        return false;
    }
}

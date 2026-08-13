using Content.Shared.Examine;
using Content.Shared.Pinpointer;

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
    [Dependency] private SharedTransformSystem _trans = default!;

    public override void Initialize() { }

    //TODO:ERRANT For testing only
    [SubscribeLocalEvent]
    private void OnExamined(Entity<SatanAlignComponent> entity, ref ExaminedEvent args)
    {
        Align(entity.AsNullable());
    }

    /// <summary>
    /// Runs align on every alignable entity.
    /// This is very expensive and should never be ran after players are in the game!
    /// </summary>
    public void AlignAll()
    {
        var query = EntityQueryEnumerator<SatanAlignComponent, TransformComponent>();

        var i = 0; //TODO:ERRANT testing only
        foreach (var (ent, comp, transform) in query)
        {
            if (!transform.Anchored)
                continue;

            Align((ent, comp));
            i++;
        }
    }

    /// <summary>
    /// Makes the target entity align to adjacent compatible entities
    /// </summary>
    /// <remarks> Only works for entities which are symmetrical, so north-south and east-west headings are interchangeable</remarks>
    private void Align(Entity<SatanAlignComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (!TryComp<SatanKindComponent>(entity, out var kind))
            return;

        var trans = Transform(entity); //TODO:ERRANT get this from the calling function

        if (!trans.Anchored)
            return;

        // var list = new List<EntityUid>();

        // locate nearby walls and doors
        var query = EntityQueryEnumerator<SatanKindComponent>();

        var ns = 0d;
        var ew = 0d;
        var neighbors = 0;

        if (!HasComp<NavMapDoorComponent>(entity))
        {
            var a = entity.Owner;
        }

        foreach (var (ent, comp) in query)
        {
            var t = Transform(ent);

            if (kind.AlignType != comp.AlignType)
                continue;

            // They must be anchored to the same parent to matter
            if (t.ParentUid != trans.ParentUid)
                continue;
            if (!t.Anchored)
                continue;

            trans.Coordinates.TryDistance(EntityManager, t.Coordinates, out var dist); //TODO:ERRANT use _trans.InRange ?
            if (dist > entity.Comp.ProximityMax || dist < entity.Comp.ProximityMin)
                continue;

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
            Log.Warning($"Airlock {entity} was not rotated");
        }


        var locRot = Math.Abs(trans.LocalRotation);
        // Don't want to "fix" a 180 degree misalignment
        // Maybe airlocks should only have 2 rot states in the first place?

        // rotate sprite
        if (!MathHelper.CloseTo(locRot, targetAngle, 0.01f)
            && !MathHelper.CloseTo(locRot, targetAngle + Angle.FromDegrees(180), 0.01f))
        {
            _trans.SetLocalRotation(entity, trans.LocalRotation + Angle.FromDegrees(90));
        }
        else
        {
            // _popup.PopupEntity("Rotation ok", entity);
        }
    }
}

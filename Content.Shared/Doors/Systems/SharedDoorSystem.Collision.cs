using Content.Shared.Doors.Components;
using Content.Shared.Physics;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem
{
    [Dependency] private FixtureSystem _fixture = default!;

    private void InitializeCollision()
    {
        SubscribeLocalEvent<DoorComponent, StartCollideEvent>(HandleCollide);
        SubscribeLocalEvent<DoorComponent, PreventCollideEvent>(PreventCollision);
    }

    /// <summary>
    /// Should the door have collision (i.e. block transit)?
    /// </summary>
    /// <param name="door">The door in question</param>
    /// <returns>True if the door is closed or currently closing, and open if the door is open or currently opening.</returns>
    private static bool IsDoorCollidable(DoorComponent door)
    {
        switch (door.State)
        {
            case DoorState.Closed:
            case DoorState.AttemptingOpenBySelf:
            case DoorState.AttemptingOpenByPrying:
            case DoorState.Closing:
            case DoorState.WeldedClosed:
            case DoorState.Denying:
            case DoorState.Emagging:
                return true;
            case DoorState.AttemptingCloseBySelf:
            case DoorState.AttemptingCloseByPrying:
            case DoorState.Open:
            case DoorState.Opening:
            default:
                return false;
        }
    }

    protected virtual void SetCollidable(Entity<DoorComponent> door,
        bool isClosed,
        OccluderComponent? occluder = null)
    {
        // If the door is a rotating door, flip which of the two door fixtures exist.
        // Otherwise

        // TODO: prediction hell :godo:
        if (TryComp<RotatingDoorComponent>(door, out var rotatingDoor))
        {
            // Thanks be to Sloth; this doesn't throw if the fixture doesn't exist.
            _fixture.DestroyFixture(door,
                isClosed ? rotatingDoor.OuterFixtureName : rotatingDoor.InnerFixtureName);

            _fixture.TryCreateFixture(door,
                isClosed ? rotatingDoor.InnerDoor : rotatingDoor.OuterDoor,
                isClosed ? rotatingDoor.InnerFixtureName : rotatingDoor.OuterFixtureName,
                rotatingDoor.Density,
                true,
                rotatingDoor.CollisionLayer,
                rotatingDoor.CollisionMask
            );
        }
        else if (TryComp<PhysicsComponent>(door, out var physics))
            _physics.SetCanCollide(door, isClosed, body: physics);

        if (!isClosed)
            door.Comp.CurrentlyCrushing.Clear();

        if (door.Comp.Occludes)
            _occluder.SetEnabled(door, isClosed, occluder);
    }

    /// <summary>
    /// Crushes everyone colliding with us by more than <see cref="IntersectPercentage"/>%.
    /// </summary>
    public void Crush(Entity<DoorComponent> door)
    {
        if (!door.Comp.CanCrush)
            return;

        // Find entities and apply crushing effects
        var stunTime = door.Comp.DoorStunTime + door.Comp.OpenTimeOne;
        foreach (var entity in GetColliding(door))
        {
            door.Comp.CurrentlyCrushing.Add(entity);
            if (door.Comp.CrushDamage != null)
                _damageableSystem.TryChangeDamage(entity, door.Comp.CrushDamage, origin: door);

            _stunSystem.TryParalyze(entity, stunTime, true);
        }

        if (door.Comp.CurrentlyCrushing.Count == 0)
            return;

        // queue the door to open so that the player is no longer stunned once it has FINISHED opening.
        door.Comp.NextStateChange = _gameTiming.CurTime + door.Comp.DoorStunTime;

        switch (door.Comp.State)
        {
            case DoorState.Closing:
                SetState(door, DoorState.Open);

                break;
            case DoorState.Opening:
                SetState(door, DoorState.Closed);

                break;
            case DoorState.Closed:
            case DoorState.AttemptingCloseBySelf:
            case DoorState.AttemptingCloseByPrying:
            case DoorState.Open:
            case DoorState.AttemptingOpenBySelf:
            case DoorState.AttemptingOpenByPrying:
            case DoorState.WeldedClosed:
            case DoorState.Denying:
            case DoorState.Emagging:
            default:
                return;
        }
    }

    /// <summary>
    ///     Get all entities that collide with this door by more than <see cref="IntersectPercentage"/> percent.
    /// </summary>
    public IEnumerable<EntityUid> GetColliding(EntityUid uid)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            yield break;

        var xform = Transform(uid);
        // Getting the world bounds from the gridUid allows us to use the version of
        // GetCollidingEntities that returns Entity<PhysicsComponent>
        if (!TryComp<MapGridComponent>(xform.GridUid, out var mapGridComp))
            yield break;
        var tileRef = _mapSystem.GetTileRef(xform.GridUid.Value, mapGridComp, xform.Coordinates);

        _doorIntersecting.Clear();
        _entityLookup.GetLocalEntitiesIntersecting(xform.GridUid.Value,
            tileRef.GridIndices,
            _doorIntersecting,
            gridComp: mapGridComp,
            flags: (LookupFlags.All & ~LookupFlags.Sensors));

        // TODO SLOTH fix electro's code.
        // ReSharper disable once InconsistentNaming

        foreach (var otherPhysics in _doorIntersecting)
        {
            if (otherPhysics.Comp == physics || !otherPhysics.Comp.CanCollide)
                continue;

            switch (otherPhysics.Comp.CollisionLayer)
            {
                //TODO: Make only shutters ignore these objects upon colliding instead of all airlocks
                // Excludes Glasslayer for windows, GlassAirlockLayer for windoors, TableLayer for tables
                case (int)CollisionGroup.GlassLayer:
                case (int)CollisionGroup.GlassAirlockLayer:
                case (int)CollisionGroup.TableLayer:
                //If the colliding entity is a slippable item ignore it by the airlock
                case (int)CollisionGroup.SlipLayer when
                    otherPhysics.Comp.CollisionMask == (int)CollisionGroup.ItemMask:
                //For when doors need to close over conveyor belts
                case (int)CollisionGroup.ConveyorMask:
                    continue;
            }

            if ((physics.CollisionMask & otherPhysics.Comp.CollisionLayer) == 0 &&
                (otherPhysics.Comp.CollisionMask & physics.CollisionLayer) == 0)
                continue;

            yield return otherPhysics.Owner;
        }
    }

    private static void PreventCollision(Entity<DoorComponent> door, ref PreventCollideEvent args)
    {
        if (!door.Comp.CurrentlyCrushing.Contains(args.OtherEntity))
            return;

        args.Cancelled = true;
    }

    /// <summary>
    /// Open a door if a player or door-bumper (PDA, ID-card) collide with the door. Sadly, bullets no longer generate
    /// "access denied" sounds as you fire at a door.
    /// </summary>
    private void HandleCollide(Entity<DoorComponent> door, ref StartCollideEvent args)
    {
        if (!door.Comp.BumpOpen)
            return;

        if (door.Comp.State is not (DoorState.Closed or DoorState.Denying))
            return;

        var otherUid = args.OtherEntity;

        if (_tag.HasTag(otherUid, DoorBumpTag))
            TryOpen(door, otherUid, quiet: door.Comp.State == DoorState.Denying, predicted: true);
    }
}

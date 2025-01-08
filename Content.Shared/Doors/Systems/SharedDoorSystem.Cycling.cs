using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Doors.Systems;

public abstract partial class SharedDoorSystem
{
    private void SetCyclingCollision(Entity<CyclingDoorComponent> door, bool isClosed)
    {
        // Rotating door collision is faked via creating a narrow human-sized hole to walk
        // through and (ab)using DoorComponent state to model which side should be open.
        // This is done to keep as much door code as possible related to DoorComponent; doors historically
        // have complexity and prediction problems.

        // We can do this via ternaries instead, but this is a bit easier to read.
        var fixtureToDestroy = door.Comp.InnerFixtureName;
        var fixtureToCreate = door.Comp.OuterFixtureName;
        var fixtureToCreateShape = door.Comp.OuterDoor;

        if (isClosed)
        {
            fixtureToDestroy = door.Comp.OuterFixtureName;
            fixtureToCreate = door.Comp.InnerFixtureName;
            fixtureToCreateShape = door.Comp.InnerDoor;
        }

        _fixture.DestroyFixture(door, fixtureToDestroy);

        _fixture.TryCreateFixture(door,
            fixtureToCreateShape,
            fixtureToCreate,
            door.Comp.Density,
            true,
            door.Comp.CollisionLayer,
            door.Comp.CollisionMask
        );

        // No-matter what, no matter what cursed setup the door has in YAML, the door does not occlude.
        // A dynamic door like this isn't able to be sensibly handled in Clyde's occlusion system without
        // causing edge cases. Ergo, assume all rotating doors are transparent.

        // TODO: Revisit this when Clyde V2 (Claudia) is real.
        // For future work: the ideal for this system is that a rotating door is a narrow player-only
        // door that is a bit claustrophobic. The ideal is that the occlusion matches the physics: a C-shaped
        // polygon matching which door is closed.
        _occluder.SetEnabled(door, false);
    }

    private void HandleCyclingBump(Entity<DoorComponent> door, StartCollideEvent args, CyclingDoorComponent cyclingDoor)
    {
        // Do nothing if the rotating door's actual doors have not been hit.
        if (args.OurFixtureId != cyclingDoor.InnerFixtureName && args.OurFixtureId != cyclingDoor.OuterFixtureName)
            return;

        switch (door.Comp.State)
        {
            case DoorState.Closed or DoorState.Denying:
                TryOpen(door, args.OtherEntity, quiet: door.Comp.State == DoorState.Denying, predicted: true);

                return;
            case DoorState.Open:
                TryClose(door, args.OtherEntity, predicted: true);

                return;
            case DoorState.AttemptingCloseBySelf
                or DoorState.AttemptingCloseByPrying
                or DoorState.Closing
                or DoorState.AttemptingOpenBySelf
                or DoorState.AttemptingOpenByPrying
                or DoorState.Opening
                or DoorState.WeldedClosed
                or DoorState.Emagging:
            default:
                return;
        }
    }
}

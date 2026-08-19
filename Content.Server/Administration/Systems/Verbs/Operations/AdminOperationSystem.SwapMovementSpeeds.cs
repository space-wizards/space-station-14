using Content.Shared.Administration.Verbs.Operations;
using Content.Shared.Administration.Verbs.Operations.Smites;
using Content.Shared.Movement.Components;

namespace Content.Server.Administration.Systems.Verbs.Operations;

public sealed partial class AdminOperationSystem
{
    [SubscribeLocalEvent]
    private void OnSwapMovementSpeeds(Entity<MetaDataComponent> entity, ref AdminOperationEvent<SwapMovementSpeedsOperation> args)
    {
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(entity);
        (movementSpeed.BaseSprintSpeed, movementSpeed.BaseWalkSpeed) =
            (movementSpeed.BaseWalkSpeed, movementSpeed.BaseSprintSpeed);

        Dirty(entity, movementSpeed);
    }
}

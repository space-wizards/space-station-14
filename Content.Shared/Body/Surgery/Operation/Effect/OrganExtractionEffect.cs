using Content.Shared.Body.Systems;
using Content.Shared.Body.Surgery.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Body.Surgery.Operation.Effect;

public sealed class OrganExtractionEffect : IOperationEffect
{
    public void Execute(EntityUid user, OperationComponent operation)
    {
        var systemMan = IoCManager.Resolve<IEntitySystemManager>();
        var body = systemMan.GetEntitySystem<SharedBodySystem>();
        if (operation.SelectedOrgan != null && body.DropOrgan(operation.SelectedOrgan))
        {
            var hands = systemMan.GetEntitySystem<SharedHandsSystem>();
            hands.TryPickupAnyHand(user, operation.SelectedOrgan.Value, animate: false);
        }
    }
}

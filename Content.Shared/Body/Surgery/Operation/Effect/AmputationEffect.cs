using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Surgery.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Body.Surgery.Operation.Effect;

public sealed class AmputationEffect : IOperationEffect
{
    public void Execute(EntityUid user, OperationComponent operation)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var systemMan = IoCManager.Resolve<IEntitySystemManager>();
        var body = systemMan.GetEntitySystem<SharedBodySystem>();
        entMan.TryGetComponent<BodyPartComponent>(operation.Part, out var part);
        if (body.OrphanPart(operation.Part, part))
        {
            var hands = systemMan.GetEntitySystem<SharedHandsSystem>();
            hands.TryPickupAnyHand(user, operation.Part, animate: false);
        }
    }
}

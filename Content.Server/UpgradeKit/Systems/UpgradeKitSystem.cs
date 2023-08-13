using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.UpgradeKit.Components;
using Content.Shared.Interaction;

namespace Content.Server.UpgradeKit.Systems;

public sealed class UpgradeKitSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UpgradeKitComponent, AfterInteractEvent>(AfterInteraction);
    }

    private void AfterInteraction(EntityUid uid, UpgradeKitComponent component, AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        var target = args.Target.Value;
        var targetProto = _entityManager.GetComponent<MetaDataComponent>(target).EntityPrototype;

        if (targetProto == null)
            return;

        if (component.BasePrototype == targetProto.ID)
        {
            var transform = _entityManager.GetComponent<TransformComponent>(target);

            var upgradedTarget = _entityManager.CreateEntityUninitialized(component.UpgradedPrototype, transform.Coordinates);
            _entityManager.InitializeAndStartEntity(upgradedTarget);

            bool targetInHand = _handsSystem.IsHolding(args.User, target, out var inHand);

            if (targetInHand)
            {
                _handsSystem.PickupOrDrop(args.User, target, true, false, false);
                _handsSystem.PickupOrDrop(args.User, upgradedTarget, true, false, false);
            }

            _popupSystem.PopupEntity(Loc.GetString("interaction-upgrade-kit-applied", ("targetName", targetProto.Name)), upgradedTarget, args.User);

            QueueDel(uid);
            QueueDel(target);
        }
    }
}

using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.UpgradeKit.Components;
using static Content.Shared.UpgradeKit.Systems.SharedUpgradeKitSystem;
using JetBrains.Annotations;

namespace Content.Server.UpgradeKit.Systems;

[UsedImplicitly]
public sealed class UpgradeKitSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UpgradeKitComponent, AfterInteractEvent>(AfterInteraction);
        SubscribeLocalEvent<UpgradeKitComponent, UpgradeKitDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(EntityUid uid, UpgradeKitComponent component, UpgradeKitDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target == null)
            return;

        var target = args.Target.Value;
        var targetProto = _entityManager.GetComponent<MetaDataComponent>(target)?.EntityPrototype;

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

    private void AfterInteraction(EntityUid uid, UpgradeKitComponent component, AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        var target = args.Target.Value;
        var targetProto = _entityManager.GetComponent<MetaDataComponent>(target)?.EntityPrototype;

        if (targetProto == null)
            return;

        var doAfterEventArgs = new DoAfterArgs(args.User, component.DoAfterTime, new UpgradeKitDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }
}

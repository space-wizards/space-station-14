using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Blocking;

public sealed class BlockingUserSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly BlockingSystem _blockingSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockingUserComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BlockingUserComponent, DamageModifyEvent>(OnUserDamageModified);

        SubscribeLocalEvent<BlockingUserComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<BlockingUserComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<BlockingUserComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnParentChanged(EntityUid uid, BlockingUserComponent component, ref EntParentChangedMessage args)
    {
        if (TryComp<BlockingComponent>(component.BlockingItem, out var blockComp) && blockComp.IsBlocking)
        {
            _blockingSystem.StopBlocking(component.BlockingItem.Value, blockComp, uid);
        }
    }

    private void OnInsertAttempt(EntityUid uid, BlockingUserComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        if (TryComp<BlockingComponent>(component.BlockingItem, out var blockComp) && blockComp.IsBlocking)
        {
            _blockingSystem.StopBlocking(component.BlockingItem.Value, blockComp, uid);
        }
    }

    private void OnAnchorChanged(EntityUid uid, BlockingUserComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        if (TryComp<BlockingComponent>(component.BlockingItem, out var blockComp) && blockComp.IsBlocking)
        {
            _blockingSystem.StopBlocking(component.BlockingItem.Value, blockComp, uid);
        }
    }

    private void OnDamageChanged(EntityUid uid, BlockingUserComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta != null && args.DamageIncreased)
        {
            _damageable.TryChangeDamage(component.BlockingItem, args.DamageDelta);
        }
    }

    private void OnUserDamageModified(EntityUid uid, BlockingUserComponent component, DamageModifyEvent args)
    {
        if (TryComp<BlockingComponent>(component.BlockingItem, out var blockingComponent))
        {
            if (_proto.TryIndex(blockingComponent.PassiveBlockDamageModifer, out DamageModifierSetPrototype? passiveblockModifier) && !blockingComponent.IsBlocking)
                args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, passiveblockModifier);

            if (_proto.TryIndex(blockingComponent.ActiveBlockDamageModifier, out DamageModifierSetPrototype? activeBlockModifier) && blockingComponent.IsBlocking)
            {
                args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, activeBlockModifier);
                SoundSystem.Play(blockingComponent.BlockSound.GetSound(), Filter.Pvs(component.Owner, entityManager: EntityManager), component.Owner, AudioHelpers.WithVariation(0.2f));
            }
        }
    }
}

using Content.Shared.Blocking.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private void InitializeUser()
    {
        SubscribeLocalEvent<BlockingUserComponent, DamageModifyEvent>(OnUserDamageModified);
        SubscribeLocalEvent<BlockingUserComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<BlockingUserComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<BlockingUserComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<BlockingUserComponent, EntityTerminatingEvent>(OnEntityTerminating);
    }

    private void OnParentChanged(EntityUid uid, BlockingUserComponent component, ref EntParentChangedMessage args)
    {
        UserStopBlocking(uid, component);
    }

    private void OnInsertAttempt(EntityUid uid, BlockingUserComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        UserStopBlocking(uid, component);
    }

    private void OnAnchorChanged(EntityUid uid, BlockingUserComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        UserStopBlocking(uid, component);
    }

    private void OnUserDamageModified(Entity<BlockingUserComponent> entity, ref DamageModifyEvent args)
    {
        if (entity.Comp.BlockingItem is not { } item || !_blockQuery.TryComp(item, out var blocking))
            return;

        if (args.Damage.GetTotal() <= 0)
            return;

        var blockFraction = blocking.IsBlocking ? blocking.ActiveBlockFraction : blocking.PassiveBlockFraction;
        blockFraction = Math.Clamp(blockFraction, 0, 1);

        // This is how much damage the shield is attempting to block
        var split = args.OriginalDamage * blockFraction;
        var damage = _damageable.ChangeDamage(item, split);

        // Of the damage that went through, reduce by the appropriate blocking modifiers.
        var modifier = GetBlockingModifier((item, blocking));
        var blowthrough = DamageSpecifier.ApplyModifierSet(split, modifier);

        args.Damage *= 1f - blockFraction;
        args.Damage += blowthrough;

        if (blocking.IsBlocking && damage.AnyPositive())
            _audio.PlayPvs(blocking.BlockSound, entity);
    }

    private void OnEntityTerminating(EntityUid uid, BlockingUserComponent component, ref EntityTerminatingEvent args)
    {
        if (!_blockQuery.TryComp(component.BlockingItem, out var blockingComponent))
            return;

        StopBlockingHelper(component.BlockingItem.Value, blockingComponent, uid);

    }

    /// <summary>
    /// Check for the shield and has the user stop blocking
    /// Used where you'd like the user to stop blocking, but also don't want to remove the <see cref="BlockingUserComponent"/>
    /// </summary>
    /// <param name="uid">The user blocking</param>
    /// <param name="component">The <see cref="BlockingUserComponent"/></param>
    private void UserStopBlocking(EntityUid uid, BlockingUserComponent component)
    {
        if (TryComp<BlockingComponent>(component.BlockingItem, out var blockComp) && blockComp.IsBlocking)
            StopBlocking(component.BlockingItem.Value, blockComp, uid);
    }
}

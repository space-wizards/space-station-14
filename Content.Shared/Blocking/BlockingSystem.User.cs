using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.Blocking;

public sealed partial class BlockingSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private void InitializeUser()
    {
        SubscribeLocalEvent<BlockingUserComponent, DamageModifyEvent>(OnUserDamageModified);
        SubscribeLocalEvent<BlockingComponent, DamageModifyEvent>(OnDamageModified);

        SubscribeLocalEvent<BlockingUserComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<BlockingUserComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<BlockingUserComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<BlockingUserComponent, EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeLocalEvent<HasRaisedShieldComponent, MoveInputEvent>(OnMoveInput);
    }

    private void OnParentChanged(Entity<BlockingUserComponent> shieldUser, ref EntParentChangedMessage args)
    {
        UserStopBlocking(shieldUser);
    }

    private void OnInsertAttempt(Entity<BlockingUserComponent> shieldUser, ref ContainerGettingInsertedAttemptEvent args)
    {
        UserStopBlocking(shieldUser);
    }

    private void OnAnchorChanged(Entity<BlockingUserComponent> shieldUser, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        UserStopBlocking(shieldUser);
    }

    private void OnMoveInput(Entity<HasRaisedShieldComponent> shieldUser, ref MoveInputEvent args)
    {
        if (!TryComp<BlockingUserComponent>(shieldUser, out var shieldUserComp))
            return;

        UserStopBlocking((shieldUser, shieldUserComp));
    }

    private void OnUserDamageModified(EntityUid uid, BlockingUserComponent component, DamageModifyEvent args)
    {
        if (component.BlockingItem is not { } item
            || !TryComp<BlockingComponent>(item, out var blocking)
            || args.Damage.GetTotal() <= 0
            // A shield should only block damage it can itself absorb. To determine that we need the Damageable component on it.
            || !TryComp<DamageableComponent>(item, out var dmgComp))
            return;

        var blockFraction = blocking.IsBlocking ? blocking.ActiveBlockFraction : blocking.PassiveBlockFraction;
        blockFraction = Math.Clamp(blockFraction, 0, 1);
        _damageable.TryChangeDamage((item, dmgComp), blockFraction * args.OriginalDamage);

        var modify = new DamageModifierSet();
        foreach (var key in dmgComp.Damage.DamageDict.Keys)
        {
            modify.Coefficients.TryAdd(key, 1 - blockFraction);
        }

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modify);

        if (blocking.IsBlocking && !args.Damage.Equals(args.OriginalDamage))
            _audio.PlayPvs(blocking.BlockSound, uid);

    }

    private void OnDamageModified(EntityUid uid, BlockingComponent component, DamageModifyEvent args)
    {
        var modifier = component.IsBlocking ? component.ActiveBlockDamageModifier : component.PassiveBlockDamageModifer;
        if (modifier == null)
        {
            return;
        }

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
    }

    private void OnEntityTerminating(Entity<BlockingUserComponent> shieldUser, ref EntityTerminatingEvent args)
    {
        if (!TryComp<BlockingComponent>(shieldUser.Comp.BlockingItem, out var blockingComponent))
            return;

        StopBlockingHelper((shieldUser.Comp.BlockingItem.Value, blockingComponent), shieldUser);

    }

    /// <summary>
    /// Check for the shield and has the user stop blocking
    /// Used where you'd like the user to stop blocking, but also don't want to remove the <see cref="BlockingUserComponent"/>
    /// </summary>
    /// <param name="shieldUser">The user entity with the shield</param>
    /// <param name="component">The <see cref="BlockingUserComponent"/></param>
    private void UserStopBlocking(Entity<BlockingUserComponent> shieldUser)
    {
        if (TryComp<BlockingComponent>(shieldUser.Comp.BlockingItem, out var blockComp) && blockComp.IsBlocking)
            StopBlocking((shieldUser.Comp.BlockingItem.Value, blockComp), shieldUser);
    }
}

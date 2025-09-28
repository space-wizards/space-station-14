using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed class BlockMovementWhenPulledStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockMovementWhenPulledStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<BlockMovementWhenPulledStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        SubscribeLocalEvent<BlockMovementWhenPulledStatusEffectComponent, StatusEffectRelayedEvent<UpdateCanMoveEvent>>(OnUpdateCanMove);
        SubscribeLocalEvent<BlockMovementWhenPulledStatusEffectComponent, StatusEffectRelayedEvent<PullStartedMessage>>(OnPullMessage);
        SubscribeLocalEvent<BlockMovementWhenPulledStatusEffectComponent, StatusEffectRelayedEvent<PullStoppedMessage>>(OnPullMessage);
    }

    private void OnStatusEffectApplied(Entity<BlockMovementWhenPulledStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _actionBlocker.UpdateCanMove(args.Target);
    }

    private void OnStatusEffectRemoved(Entity<BlockMovementWhenPulledStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _actionBlocker.UpdateCanMove(args.Target);
    }

    private void OnPullMessage<T>(Entity<BlockMovementWhenPulledStatusEffectComponent> ent, ref StatusEffectRelayedEvent<T> args)
    {
        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } target)
            return;

        _actionBlocker.UpdateCanMove(target);
    }

    private void OnUpdateCanMove(Entity<BlockMovementWhenPulledStatusEffectComponent> ent, ref StatusEffectRelayedEvent<UpdateCanMoveEvent> args)
    {
        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } target)
            return;

        if (!TryComp<PullableComponent>(target, out var pullable) || !pullable.BeingPulled)
            return;

        args.Args.Cancel();
    }
}

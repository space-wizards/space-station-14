using Content.Shared.ActionBlocker;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Magic.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Pointing;

namespace Content.Shared.Magic.Systems;

public abstract class PetrifySpellSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PetrifiedComponent, MapInitEvent>(OnPetrify);
        SubscribeLocalEvent<PetrifiedStatueComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PetrifiedStatueComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PetrifiedStatueComponent, AnimateSpellEvent>(OnAnimate);

        SubscribeLocalEvent<PetrifiedStatueComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, UpdateCanMoveEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, PointAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, DropAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, IsEquippingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, IsUnequippingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, InteractionAttemptEvent>(OnAttemptInteract);
    }

    protected virtual void OnPetrify(Entity<PetrifiedComponent> ent, ref MapInitEvent args)
    {
        var ev = new PetrifySpellEvent();
        RaiseLocalEvent(ref ev);
    }

    protected virtual void OnStartup(EntityUid ent, PetrifiedStatueComponent comp, ComponentStartup args)
    {
        _blocker.UpdateCanMove(ent);
    }

    protected virtual void OnShutdown(EntityUid ent, PetrifiedStatueComponent comp, ComponentShutdown args)
    {
        _blocker.UpdateCanMove(ent);
    }

    private void OnAnimate(Entity<PetrifiedStatueComponent> ent, ref AnimateSpellEvent args)
    {
        Log.Debug("hi golem time");
        // TODO: Stone golem
    }

    private void OnAttempt(EntityUid ent, PetrifiedStatueComponent comp, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    private void OnAttemptInteract(Entity<PetrifiedStatueComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }
}

[ByRefEvent]
public readonly record struct PetrifySpellEvent;

using Content.Shared.ActionBlocker;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Magic.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Pointing;
using Content.Shared.Slippery;

namespace Content.Shared.Magic.Systems;

public abstract class PetrifySpellSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PetrifiedComponent, MapInitEvent>(OnPetrify);
        SubscribeLocalEvent<PetrifiedStatueComponent, ComponentStartup>(OnStartup);
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
        RaiseLocalEvent(ent, ref ev);
    }

    protected virtual void OnStartup(EntityUid ent, PetrifiedStatueComponent comp, ComponentStartup args)
    {
        //_blocker.UpdateCanMove(ent);
    }

    protected virtual void OnAnimate(Entity<PetrifiedStatueComponent> ent, ref AnimateSpellEvent args)
    {
        // Stone Golem

        //RemComp<PetrifiedStatueComponent>(ent);
        //RemComp<NoSlipComponent>(ent);
        //RemComp<CanMoveInAirComponent>(ent);
        //RemComp<MovementAlwaysTouchingComponent>(ent);

        // Other than PetrifiedStatue, the components above are added by the Animate spell - and then removed here.
        // It would be way better to just never add them at all if the entity is a petrified statue.
        // I don't know the best practice or method for customizing behavior of an action depending on the type of target.

        //_blocker.UpdateCanMove(ent);
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

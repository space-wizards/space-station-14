using Content.Shared.Emoting;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Magic.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Pointing;
using Content.Shared.Speech;

namespace Content.Shared.Magic.Systems;

public abstract class PetrifySpellSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PetrifiedComponent, MapInitEvent>(OnPetrify);
        SubscribeLocalEvent<StoneStatueComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PetrifiedStatueComponent, AnimateSpellEvent>(OnAnimate);

        SubscribeLocalEvent<PetrifiedStatueComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, UpdateCanMoveEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, PointAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, DropAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, IsEquippingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, IsUnequippingAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, SpeakAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, EmoteAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, InteractionAttemptEvent>(OnAttemptInteract);
    }

    protected virtual void OnPetrify(Entity<PetrifiedComponent> ent, ref MapInitEvent args)
    {
        var ev = new PetrifySpellEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    protected virtual void OnStartup(EntityUid ent, StoneStatueComponent comp, ComponentStartup args)
    {
        // Shader handled in ClientPetrifySpellSystem
    }

    protected virtual void OnAnimate(Entity<PetrifiedStatueComponent> ent, ref AnimateSpellEvent args)
    {
        // Stone Golem handled in ServerPetrifySpellSystem
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

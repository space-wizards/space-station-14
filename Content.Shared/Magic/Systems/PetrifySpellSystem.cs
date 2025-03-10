using Content.Shared.ActionBlocker;
using Content.Shared.Interaction.Events;
using Content.Shared.Magic.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Magic.Systems;

public abstract class PetrifySpellSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PetrifiedComponent, MapInitEvent>(OnPetrify);
        SubscribeLocalEvent<PetrifiedStatueComponent, ComponentStartup>(UpdateCanMove);
        SubscribeLocalEvent<PetrifiedStatueComponent, ComponentShutdown>(UpdateCanMove);
        SubscribeLocalEvent<PetrifiedStatueComponent, AnimateSpellEvent>(OnAnimate);

        SubscribeLocalEvent<PetrifiedStatueComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<PetrifiedStatueComponent, UpdateCanMoveEvent>(OnAttempt);
    }

    protected virtual void OnPetrify(Entity<PetrifiedComponent> ent, ref MapInitEvent args)
    {
        var ev = new PetrifySpellEvent();
        RaiseLocalEvent(ref ev);
    }

    private void UpdateCanMove(EntityUid ent, PetrifiedStatueComponent comp, EntityEventArgs args)
    {
        _blocker.UpdateCanMove(ent);
    }

    protected virtual void OnAnimate(Entity<PetrifiedStatueComponent> ent, ref AnimateSpellEvent args)
    {
        // TODO: Stone golem
    }

    private void OnAttempt(EntityUid ent, PetrifiedStatueComponent comp, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }
}

[ByRefEvent]
public readonly record struct PetrifySpellEvent;

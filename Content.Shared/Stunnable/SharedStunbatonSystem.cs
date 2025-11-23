using Content.Shared.ActionBlocker;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Stunnable;

public abstract class SharedStunbatonSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunbatonComponent, ItemToggleActivateAttemptEvent>(TryTurnOn);
        SubscribeLocalEvent<StunbatonComponent, ItemToggleDeactivateAttemptEvent>(TryTurnOff);
    }

    protected virtual void TryTurnOn(Entity<StunbatonComponent> entity, ref ItemToggleActivateAttemptEvent args)
    {
        if (args.User != null && !_actionBlocker.CanComplexInteract(args.User.Value)) {
            args.Cancelled = true;
            return;
        }
    }

    protected virtual void TryTurnOff(Entity<StunbatonComponent> entity, ref ItemToggleDeactivateAttemptEvent args)
    {
        if (args.User != null && !_actionBlocker.CanComplexInteract(args.User.Value)) {
            args.Cancelled = true;
            return;
        }
    }
}

using Content.Shared.Explosion.Components;

namespace Content.Server.Explosion.EntitySystems;

public sealed class TriggerOnActionSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnActionComponent, TriggerActionEvent>(OnActionUsed);
    }

    private void OnActionUsed(Entity<TriggerOnActionComponent> ent, ref TriggerActionEvent args)
    {
        _trigger.Trigger(ent);
        args.Handled = true;
    }
}

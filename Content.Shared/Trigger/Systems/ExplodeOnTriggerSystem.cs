using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class ExplodeOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExplodeOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<ExplodeOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        _explosion.TriggerExplosive(target.Value, user: args.User);
        args.Handled = true;
    }
}

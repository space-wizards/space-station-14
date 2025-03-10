using Content.Server.Polymorph.Components;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Polymorph.Systems;

public sealed partial class PolymorphSystem
{
    private void InitializeTrigger()
    {
        SubscribeLocalEvent<PolymorphOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<PolymorphOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.User == null)
            return;

        PolymorphEntity(args.User.Value, ent.Comp.Polymorph);

        args.Handled = true;
    }
}

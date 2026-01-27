using Content.Server.Explosion.Components;
using Content.Shared.Throwing;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerOnRotSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnRotComponent, BeginRottingEvent>(OnRot);
    }

    private void OnRot(Entity<TriggerOnRotComponent> ent, ref BeginRottingEvent args)
    {
        Trigger.Trigger(ent.Owner, null, ent.Comp.KeyOut, predicted: false);
    }
}

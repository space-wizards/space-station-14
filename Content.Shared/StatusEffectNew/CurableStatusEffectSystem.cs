using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared.StatusEffectNew;

/// <summary>
/// Handles killing status effects that have a <see cref="CurableStatusEffectComponent" />.
/// </summary>
public sealed class CurableStatusEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CurableStatusEffectComponent, StatusEffectRelayedEvent<RejuvenateEvent>>(OnRejuvenate);
    }

    private void OnRejuvenate(Entity<CurableStatusEffectComponent> ent,
        ref StatusEffectRelayedEvent<RejuvenateEvent> args)
    {
        PredictedQueueDel(ent.Owner);
    }
}

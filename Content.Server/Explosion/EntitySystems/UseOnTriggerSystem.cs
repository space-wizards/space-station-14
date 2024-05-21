using Content.Server.Explosion.Components;
using Content.Shared.Interaction;

namespace Content.Server.Explosion.EntitySystems;

public sealed class UseOnTriggerSystem : EntitySystem
{
    private readonly SharedInteractionSystem _interact = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    public void OnTrigger(EntityUid uid, UseOnTriggerComponent comp, TriggerEvent args)
    {
        _interact.InteractionActivate(uid, uid, false, true, false);
    }
}

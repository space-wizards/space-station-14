using Content.Shared._Starlight.Antags.TerrorSpider;
using Content.Shared.Damage.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Spider;
using Content.Shared.Stealth.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Starlight.Antags.TerrorSpider;

public sealed class StealthOnWebSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<StealthOnWebComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<StealthOnWebComponent, EndCollideEvent>(OnEntityExit);
    }

    private void OnEntityExit(Entity<StealthOnWebComponent> ent, ref EndCollideEvent args)
    {
        if (_timing.InPrediction) return;
        if (!HasComp<SpiderWebObjectComponent>(args.OtherEntity)) return;
        ent.Comp.Collisions = Math.Max(ent.Comp.Collisions - 1, 0);
        if (ent.Comp.Collisions == 0)
        {
            RemComp<StealthComponent>(ent.Owner);
            RemComp<StealthOnMoveComponent>(ent.Owner);
            RemComp<StaminaDamageOnHitComponent>(ent.Owner);
        }
    }
    private void OnEntityEnter(Entity<StealthOnWebComponent> ent, ref StartCollideEvent args)
    {
        if (_timing.InPrediction) return;
        if (!HasComp<SpiderWebObjectComponent>(args.OtherEntity)) return;
        ent.Comp.Collisions++;
        EnsureComp<StealthComponent>(ent.Owner);
        EnsureComp<StealthOnMoveComponent>(ent.Owner);
        var staminaOnHit = EnsureComp<StaminaDamageOnHitComponent>(ent.Owner);
        staminaOnHit.Damage = 200;
    }
}

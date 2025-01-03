using Content.Shared._Starlight.Antags.TerrorSpider;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Spider;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Verbs;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Starlight.Antags.TerrorSpider;

public sealed class StealthOnWebSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<StealthOnWebComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<StealthOnWebComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<StealthOnWebComponent, StaminaMeleeHitEvent>(OnHit);

    }

    private void OnHit(Entity<StealthOnWebComponent> ent, ref StaminaMeleeHitEvent args)
    {
        if (!TryComp<StealthComponent>(ent.Owner, out var stealth))
        {
            args.Multiplier *= 0.1f;
            return;
        }

        var t = (_stealth.GetVisibility(ent.Owner) - stealth.MinVisibility) / (stealth.MaxVisibility - stealth.MinVisibility);
        args.Multiplier *= Math.Clamp(1 - t, 0.1f, 1f);
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
    }
}

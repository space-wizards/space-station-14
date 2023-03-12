using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Shared.Timing;

namespace Content.Server.Dice;

[RegisterComponent]
public sealed class ChaosDiceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChaosDiceComponent, DiceRollEvent>(OnDiceRoll);
    }

    private void OnDiceRoll(EntityUid uid, ChaosDiceComponent component, ref DiceRollEvent args)
    {
        if (_timing.CurTime > component.lastActivated + TimeSpan.FromSeconds(component.Cooldown))
            return;
        component.lastActivated = _timing.CurTime;
        var power = 1.0f * args.Die.CurrentValue / (args.Die.Sides * args.Die.Multiplier);
        if (TryComp<IgniteArtifactComponent>(uid, out var igniteEffect))
        {
            igniteEffect.Range = 3 * power;
            igniteEffect.MinFireStack = 2 * (int) power;
            igniteEffect.MaxFireStack = 4 * (int) power;
        }
        if (TryComp<ThrowArtifactComponent>(uid, out var throwEffect))
        {
            throwEffect.Range = 3 * power;
            throwEffect.TilePryChance = power;
            throwEffect.ThrowStrength = 8 * power;
        }
        if (TryComp<DamageNearbyArtifactComponent>(uid, out var damageEffect))
        {
            damageEffect.Radius = 3 * power;
            damageEffect.DamageChance = power;
            damageEffect.Damage.Clamp(10 * power, 10 * power);
        }
        var ev = new ArtifactActivatedEvent();
        RaiseLocalEvent(uid, ev);
    }
}

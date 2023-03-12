using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Shared.Timing;

namespace Content.Server.Dice;

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
        if (_timing.CurTime < component.lastActivated + TimeSpan.FromSeconds(component.Cooldown))
            return;
        component.lastActivated = _timing.CurTime;
        var power = 1.0f * args.Die.CurrentValue / (args.Die.Sides * args.Die.Multiplier);
        if (TryComp<IgniteArtifactComponent>(uid, out var igniteEffect))
        {
            igniteEffect.Range = 2 * power;
            igniteEffect.MinFireStack = (int) (2 * power);
            igniteEffect.MaxFireStack = (int) (4 * power);
        }
        if (TryComp<ThrowArtifactComponent>(uid, out var throwEffect))
        {
            throwEffect.Range = 2 * power;
            throwEffect.TilePryChance = power;
            throwEffect.ThrowStrength = 8 * power;
        }
        var ev = new ArtifactActivatedEvent();
        RaiseLocalEvent(uid, ev);
    }
}

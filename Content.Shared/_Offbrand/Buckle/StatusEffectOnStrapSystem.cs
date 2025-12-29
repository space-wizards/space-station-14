using Content.Shared.Buckle.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.Buckle;

public sealed class StatusEffectOnStrapSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectOnStrapComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<StatusEffectOnStrapComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<StatusEffectOnStrapComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void UpdateStatus(Entity<StrapComponent, StatusEffectOnStrapComponent> ent, EntityUid buckled)
    {
        var isBuckled = ent.Comp1.BuckledEntities.Contains(buckled);
        var isPowered = _powerReceiver.IsPowered(ent.Owner);

        if (isBuckled && isPowered)
        {
            if (!_statusEffects.HasStatusEffect(buckled, ent.Comp2.StatusEffect))
                _statusEffects.TryUpdateStatusEffectDuration(buckled, ent.Comp2.StatusEffect, out _);
        }
        else
        {
            _statusEffects.TryRemoveStatusEffect(buckled, ent.Comp2.StatusEffect);
        }
    }

    private void OnStrapped(Entity<StatusEffectOnStrapComponent> ent, ref StrappedEvent args)
    {
        UpdateStatus((ent.Owner, Comp<StrapComponent>(ent), ent.Comp), args.Buckle);
    }

    private void OnUnstrapped(Entity<StatusEffectOnStrapComponent> ent, ref UnstrappedEvent args)
    {
        UpdateStatus((ent.Owner, Comp<StrapComponent>(ent), ent.Comp), args.Buckle);
    }

    private void OnPowerChanged(Entity<StatusEffectOnStrapComponent> ent, ref PowerChangedEvent args)
    {
        var strap = Comp<StrapComponent>(ent);
        foreach (var entity in strap.BuckledEntities)
        {
            UpdateStatus((ent.Owner, strap, ent.Comp), entity);
        }
    }
}

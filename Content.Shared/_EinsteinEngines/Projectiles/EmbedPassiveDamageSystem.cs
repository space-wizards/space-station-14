using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Shared.Projectiles;

public sealed class EmbedPassiveDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmbedPassiveDamageComponent, DamageOtherOnHitStartupEvent>(OnDamageOtherOnHitStartup);
        SubscribeLocalEvent<ItemToggleEmbedPassiveDamageComponent, ItemToggleDamageOtherOnHitStartupEvent>(OnItemToggleStartup);
        SubscribeLocalEvent<EmbedPassiveDamageComponent, EmbedEvent>(OnEmbed);
        SubscribeLocalEvent<EmbedPassiveDamageComponent, RemoveEmbedEvent>(OnRemoveEmbed);
        SubscribeLocalEvent<EmbedPassiveDamageComponent, ItemToggledEvent>(OnItemToggle);
    }

    /// <summary>
    ///   Inherit stats from DamageOtherOnHit.
    /// </summary>
    private void OnDamageOtherOnHitStartup(EntityUid uid, EmbedPassiveDamageComponent component, DamageOtherOnHitStartupEvent args)
    {
        if (component.Damage.Empty)
            component.Damage = args.Weapon.Comp.Damage * component.ThrowingDamageMultiplier;
    }

    /// <summary>
    ///   Inherit stats from ItemToggleDamageOtherOnHit.
    /// </summary>
    private void OnItemToggleStartup(EntityUid uid, ItemToggleEmbedPassiveDamageComponent component, ItemToggleDamageOtherOnHitStartupEvent args)
    {
        if (!TryComp<EmbedPassiveDamageComponent>(uid, out var embedPassiveDamage) ||
            component.ActivatedDamage != null ||
            !(args.Weapon.Comp.ActivatedDamage is {} activatedDamage))
            return;

        component.ActivatedDamage = activatedDamage * embedPassiveDamage.ThrowingDamageMultiplier;
    }

    private void OnEmbed(EntityUid uid, EmbedPassiveDamageComponent component, EmbedEvent args)
    {
        if (component.Damage.Empty || component.Damage.GetTotal() == 0 ||
            !TryComp<MobStateComponent>(args.Embedded, out var mobState) ||
            !TryComp<DamageableComponent>(args.Embedded, out var damageable))
            return;

        component.Embedded = args.Embedded;
        component.EmbeddedDamageable = damageable;
        component.EmbeddedMobState = mobState;
        component.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(1f);

        Dirty(uid, component);
    }

    private void OnRemoveEmbed(EntityUid uid, EmbedPassiveDamageComponent component, RemoveEmbedEvent args)
    {
        component.Embedded = null;
        component.EmbeddedDamageable = null;
        component.EmbeddedMobState = null;
        component.NextDamage = TimeSpan.Zero;

        Dirty(uid, component);
    }

    /// <summary>
    ///   Used to update the EmbedPassiveDamageComponent component on item toggle.
    /// </summary>
    private void OnItemToggle(EntityUid uid, EmbedPassiveDamageComponent component, ItemToggledEvent args)
    {
        if (!TryComp<ItemToggleEmbedPassiveDamageComponent>(uid, out var itemTogglePassiveDamage))
            return;

        if (args.Activated && itemTogglePassiveDamage.ActivatedDamage is {} activatedDamage)
        {
            itemTogglePassiveDamage.DeactivatedDamage ??= component.Damage;
            component.Damage = activatedDamage;
        }
        else if (itemTogglePassiveDamage.DeactivatedDamage is {} deactivatedDamage)
            component.Damage = deactivatedDamage;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<EmbedPassiveDamageComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Embedded is null ||
                comp.EmbeddedDamageable is null ||
                comp.NextDamage > curTime || // Make sure they're up for a damage tick
                comp.EmbeddedMobState is null ||
                comp.EmbeddedMobState.CurrentState == MobState.Dead) // Don't damage dead mobs, they've already gone through too much
                continue;

            comp.NextDamage = curTime + TimeSpan.FromSeconds(1f);

            _damageable.TryChangeDamage(comp.Embedded, comp.Damage, false, false, comp.EmbeddedDamageable);
        }
    }
}

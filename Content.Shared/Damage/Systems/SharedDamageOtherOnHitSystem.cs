using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Damage.Systems;

public abstract partial class SharedDamageOtherOnHitSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DamageExamineSystem _damageExamine = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOtherOnHitComponent, DamageExamineEvent>(OnDamageExamine);
        SubscribeLocalEvent<DamageOtherOnHitComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
    }

    private void OnDamageExamine(Entity<DamageOtherOnHitComponent> ent, ref DamageExamineEvent args)
    {
        _damageExamine.AddDamageExamine(args.Message, _damageable.ApplyUniversalAllModifiers(ent.Comp.Damage * _damageable.UniversalThrownDamageModifier), Loc.GetString("damage-throw"));
    }

    /// <summary>
    /// Prevent players with the Pacified status effect from throwing things that deal damage.
    /// </summary>
    private void OnAttemptPacifiedThrow(Entity<DamageOtherOnHitComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        args.Cancel("pacified-cannot-throw");
    }

    /// <summary>
    /// Prevent immediately throwing this weapon after using it for a melee attack.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMeleeCollideHit(Entity<DamageOtherOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!TryComp<HandsComponent>(args.User, out var hands) || !TryComp<MeleeWeaponComponent>(ent.Owner, out var melee))
            return;

        if (hands.NextThrowTime < melee.NextAttack)
            _hands.ResetThrowCooldown(args.User, hands, melee.NextAttack);
    }
}

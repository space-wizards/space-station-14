using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Robust.Shared.Utility;

namespace Content.Shared.Damage.Systems;

public abstract class SharedDamageOtherOnHitSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DamageExamineSystem _damageExamine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOtherOnHitComponent, DamageExamineEvent>(OnDamageExamine);
        SubscribeLocalEvent<DamageOtherOnHitComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
    }

    private void OnDamageExamine(Entity<DamageOtherOnHitComponent> ent, ref DamageExamineEvent args)
    {
        var ev = new CollideDamageExamineEvent(ent.Comp.Damage);
        RaiseLocalEvent(ent, ref ev);
        _damageExamine.AddDamageExamine(args.Message, _damageable.ApplyUniversalAllModifiers(ev.Damage * _damageable.UniversalThrownDamageModifier), Loc.GetString("damage-throw"));
    }

    /// <summary>
    /// Prevent players with the Pacified status effect from throwing things that deal damage.
    /// </summary>
    private void OnAttemptPacifiedThrow(Entity<DamageOtherOnHitComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        args.Cancel("pacified-cannot-throw");
    }

    [ByRefEvent]
    public readonly record struct CollideDamageExamineEvent(DamageSpecifier Damage);
}

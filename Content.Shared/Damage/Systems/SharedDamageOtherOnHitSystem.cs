using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Robust.Shared.Utility;

namespace Content.Shared.Damage.Systems;

public abstract partial class SharedDamageOtherOnHitSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DamageExamineSystem _damageExamine = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOtherOnHitComponent, DamageExamineEvent>(OnDamageExamine);
        SubscribeLocalEvent<DamageOtherOnHitComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
    }

    private void OnDamageExamine(Entity<DamageOtherOnHitComponent> ent, ref DamageExamineEvent args)
    {
        _damageExamine.AddDamageExamine(args.Message, _damageable.ApplyUniversalAllModifiers(ent.Comp.Damage * _damageable.UniversalThrownDamageModifier), Loc.GetString("damage-throw"));
        if (ent.Comp.IgnoreResistances)
        {
            var msg = new FormattedMessage();
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString(ent.Comp.IgnoreResistancesMessage));
            args.Message.AddMessage(msg);
        }
    }

    /// <summary>
    /// Prevent players with the Pacified status effect from throwing things that deal damage.
    /// </summary>
    private void OnAttemptPacifiedThrow(Entity<DamageOtherOnHitComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        args.Cancel("pacified-cannot-throw");
    }
}

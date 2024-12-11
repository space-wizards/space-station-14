using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Events;
using DamageOtherOnHitComponent = Content.Shared.Damage.Components.DamageOtherOnHitComponent;

namespace Content.Shared.Damage.Systems;

public abstract class SharedDamageOtherOnHitSystem : EntitySystem
{
    [Dependency] private readonly DamageExamineSystem _damageExamine = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageOtherOnHitComponent, DamageExamineEvent>(OnDamageExamine);
        SubscribeLocalEvent<DamageOtherOnHitComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
    }

    private void OnDamageExamine(EntityUid uid, DamageOtherOnHitComponent component, ref DamageExamineEvent args)
    {
        _damageExamine.AddDamageExamine(args.Message, component.Damage, Loc.GetString("damage-throw"));
    }

    /// <summary>
    /// Prevent players with the Pacified status effect from throwing things that deal damage.
    /// </summary>
    private void OnAttemptPacifiedThrow(Entity<DamageOtherOnHitComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        args.Cancel("pacified-cannot-throw");
    }
}

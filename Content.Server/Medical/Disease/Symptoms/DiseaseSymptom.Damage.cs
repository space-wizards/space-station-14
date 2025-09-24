using Content.Shared.Damage;
using Content.Shared.Medical.Disease;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomDamage : SymptomBehavior
{
    /// <summary>
    /// Damage to apply across one or more types.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage { get; private set; } = new();
}

public sealed partial class SymptomDamage
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    /// <summary>
    /// Applies configured damage to the carrier.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype disease)
    {
        if (Damage == null || Damage.Empty)
            return;

        _damageable.TryChangeDamage(uid, new DamageSpecifier(Damage));
    }
}

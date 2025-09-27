using System;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.Medical.Disease;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomStatusEffect : SymptomBehavior
{
    /// <summary>
    /// List of effects to execute on symptom trigger. Supports any <see cref="EntityEffect"/>.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects { get; private set; } = Array.Empty<EntityEffect>();
}

public sealed partial class SymptomStatusEffect
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    /// <summary>
    /// Executes the status effects.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype disease)
    {
        if (Effects.Length == 0)
            return;

        var args = new EntityEffectBaseArgs(uid, _entMan);
        foreach (var effect in Effects)
        {
            if (!EntityEffectExt.ShouldApply(effect, args))
                continue;

            effect.Effect(args);
        }
    }
}

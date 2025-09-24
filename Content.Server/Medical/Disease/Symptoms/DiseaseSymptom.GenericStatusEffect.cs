using System;
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.Medical.Disease;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomGenericStatusEffect : SymptomBehavior
{
    /// <summary>
    /// Prototype ID of the status effect entity to apply. Must be an entity with <see cref="StatusEffectComponent"/>.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EffectProto { get; private set; }

    /// <summary>
    /// Duration in seconds for the status effect. Behavior depends on <see cref="Refresh"/> and <see cref="Type"/>.
    /// </summary>
    [DataField]
    public float Time { get; private set; } = 2.0f;

    /// <summary>
    /// true - refresh to greater value; false - accumulate.
    /// Only used when <see cref="Type"/> is Add.
    /// </summary>
    [DataField]
    public bool Refresh { get; private set; } = true;

    /// <summary>
    /// How to modify the status effect time <see cref="StatusEffectSymptomType"/>.
    /// </summary>
    [DataField]
    public StatusEffectSymptomType Type { get; private set; } = StatusEffectSymptomType.Add;
}

public sealed partial class SymptomGenericStatusEffect
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    /// <summary>
    /// Adds an effect status component to the entity.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype disease)
    {
        var duration = TimeSpan.FromSeconds(Time);

        switch (Type)
        {
            case StatusEffectSymptomType.Add:
                if (Refresh)
                    _status.TryUpdateStatusEffectDuration(uid, EffectProto, duration);
                else
                    _status.TryAddStatusEffectDuration(uid, EffectProto, duration);
                break;

            case StatusEffectSymptomType.Remove:
                _status.TryAddTime(uid, EffectProto, -duration);
                break;

            case StatusEffectSymptomType.Set:
                _status.TrySetStatusEffectDuration(uid, EffectProto, duration);
                break;
        }
    }

    public enum StatusEffectSymptomType
    {
        Add,
        Remove,
        Set
    }
}

using System;
using Content.Shared.Bed.Sleep;
using Content.Shared.Medical.Disease;
using Robust.Shared.Random;
using Content.Shared.StatusEffectNew;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomNarcolepsy : SymptomBehavior
{
    /// <summary>
    /// Forced sleep duration in seconds.
    /// </summary>
    [DataField]
    public float SleepDuration { get; private set; } = 6.0f;
}

public sealed partial class SymptomNarcolepsy
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    /// <summary>
    /// Randomly forces the carrier to fall asleep for a configured duration.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype disease)
    {
        var dur = TimeSpan.FromSeconds(SleepDuration);
        _status.TryAddStatusEffectDuration(uid, SleepingSystem.StatusEffectForcedSleeping, dur);
    }
}

using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Interaction.Events;

/// <summary>
///     Raised Directed at an entity to check whether they will handle the suicide.
/// </summary>
public sealed class SuicideEvent(EntityUid victim) : HandledEntityEventArgs
{
    public DamageSpecifier? DamageSpecifier;
    public ProtoId<DamageTypePrototype>? DamageType;
    public EntityUid Victim { get; private set; } = victim;
}

public sealed class SuicideByEnvironmentEvent(EntityUid victim) : HandledEntityEventArgs
{
    public EntityUid Victim { get; } = victim;
}

/// <summary>
/// Raised prior to a suicide. Any systems looking to block suicide should handle this event.
/// This event should NOT be used to perform an action on a suicide!
/// </summary>
public sealed class SuicideAttemptEvent : HandledEntityEventArgs;

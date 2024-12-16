using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Interaction.Events;

/// <summary>
///     Raised Directed at an entity to check whether they will handle the suicide.
/// </summary>
public sealed class SuicideEvent : HandledEntityEventArgs
{
    public SuicideEvent(EntityUid victim)
    {
        Victim = victim;
    }

    public DamageSpecifier? DamageSpecifier;
    public ProtoId<DamageTypePrototype>? DamageType;
    public EntityUid Victim { get; private set; }
}

public sealed class SuicideByEnvironmentEvent : HandledEntityEventArgs
{
    public SuicideByEnvironmentEvent(EntityUid victim)
    {
        Victim = victim;
    }

    public EntityUid Victim { get; set; }
}

public sealed class SuicideGhostEvent : HandledEntityEventArgs
{
    public SuicideGhostEvent(EntityUid victim)
    {
        Victim = victim;
    }

    public EntityUid Victim { get; set; }
    public bool CanReturnToBody;
}

using Robust.Shared.Audio;

namespace Content.Shared.Beam.Components;
/// <summary>
/// Use this as a generic beam. Not for something like a laser gun, more for something continuous like lightning.
/// </summary>
public abstract class SharedBeamComponent : Component
{
    /// <summary>
    /// A unique list of targets that this beam collided with.
    /// Useful for code like Arcing in the Lightning Component.
    /// </summary>
    [ViewVariables]
    [DataField("hitTargets")]
    public HashSet<EntityUid> HitTargets = new();

    /// <summary>
    /// The virtual entity representing a beam.
    /// </summary>
    [ViewVariables]
    [DataField("virtualBeamController")]
    public EntityUid? VirtualBeamController;

    /// <summary>
    /// The first beam created, useful for keeping track of chains.
    /// </summary>
    [ViewVariables]
    [DataField("originBeam")]
    public EntityUid OriginBeam;

    /// <summary>
    /// The entity that fired the beam originally
    /// </summary>
    [ViewVariables]
    [DataField("beamShooter")]
    public EntityUid BeamShooter;

    /// <summary>
    /// A unique list of created beams that the controller keeps track of.
    /// </summary>
    [ViewVariables]
    [DataField("createdBeams")]
    public HashSet<EntityUid> CreatedBeams = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sound")]
    public SoundSpecifier? Sound;
}

/// <summary>
/// Called where a Beam Controller is first created. Stores the originator beam euid and the controller euid.
/// </summary>
public sealed class BeamControllerCreatedEvent : EntityEventArgs
{
    public EntityUid OriginBeam;
    public EntityUid BeamControllerEntity;

    public BeamControllerCreatedEvent(EntityUid originBeam, EntityUid beamControllerEntity)
    {
        OriginBeam = originBeam;
        BeamControllerEntity = beamControllerEntity;
    }
}

/// <summary>
/// Called after TryCreateBeam succeeds.
/// </summary>
public sealed class CreateBeamSuccessEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Target;

    public CreateBeamSuccessEvent(EntityUid user, EntityUid target)
    {
        User = user;
        Target = target;
    }
}

/// <summary>
/// Called once the beam is fully created
/// </summary>
public sealed class BeamFiredEvent : EntityEventArgs
{
    public readonly EntityUid CreatedBeam;

    public BeamFiredEvent(EntityUid createdBeam)
    {
        CreatedBeam = createdBeam;
    }
}

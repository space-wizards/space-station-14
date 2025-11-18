using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Beam.Components;
/// <summary>
/// Use this as a generic beam. Not for something like a laser gun, more for something continuous like lightning.
/// </summary>
public abstract partial class SharedBeamComponent : Component
{
    /// <summary>
    /// A unique list of targets that this beam collided with.
    /// Useful for code like Arcing in the Lightning Component.
    /// </summary>
    [DataField("hitTargets")]
    public HashSet<EntityUid> HitTargets = new();

    /// <summary>
    /// The virtual entity representing a beam.
    /// </summary>
    [DataField("virtualBeamController")]
    public EntityUid? VirtualBeamController;

    /// <summary>
    /// The first beam created, useful for keeping track of chains.
    /// </summary>
    [DataField("originBeam")]
    public EntityUid OriginBeam;

    /// <summary>
    /// The entity that fired the beam originally
    /// </summary>
    [DataField("beamShooter")]
    public EntityUid BeamShooter;

    /// <summary>
    /// A unique list of created beams that the controller keeps track of.
    /// </summary>
    [DataField("createdBeams")]
    public HashSet<EntityUid> CreatedBeams = new();

    /// <summary>
    /// Sound played upon creation
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("sound")]
    public SoundSpecifier? Sound;
}

/// <summary>
/// Called where a <see cref="BeamControllerEntity"/> is first created. Stores the originator beam euid and the controller euid.
/// Raised on the <see cref="BeamControllerEntity"/> and broadcast.
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

/// <summary>
/// Raised on the new entity created after the <see cref="SharedBeamSystem"/> creates one.
/// Used to get sprite data over to the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class BeamVisualizerEvent : EntityEventArgs
{
    public readonly NetEntity Beam;
    public readonly float DistanceLength;
    public readonly Angle UserAngle;
    public readonly string? BodyState;
    public readonly string Shader = "unshaded";

    public BeamVisualizerEvent(NetEntity beam, float distanceLength, Angle userAngle, string? bodyState = null, string shader = "unshaded")
    {
        Beam = beam;
        DistanceLength = distanceLength;
        UserAngle = userAngle;
        BodyState = bodyState;
        Shader = shader;
    }
}

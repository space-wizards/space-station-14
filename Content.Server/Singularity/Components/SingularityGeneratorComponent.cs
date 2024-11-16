using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

using Content.Server.Singularity.EntitySystems;
using Content.Shared.Physics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Singularity.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(SingularityGeneratorSystem))]
public sealed partial class SingularityGeneratorComponent : Component
{
    /// <summary>
    /// The amount of power this generator has accumulated.
    /// If you want to set this use <see  cref="SingularityGeneratorSystem.SetPower"/>
    /// </summary>
    [DataField]
    public float Power = 0;

    /// <summary>
    /// The power threshold at which this generator will spawn a singularity.
    /// If you want to set this use <see  cref="SingularityGeneratorSystem.SetThreshold"/>
    /// </summary>
    [DataField]
    public float Threshold = 16;

    /// <summary>
    /// Allows the generator to ignore all the failsafe stuff, e.g. when emagged
    /// </summary>
    [DataField]
    public bool FailsafeDisabled = false;

    /// <summary>
    /// Maximum distance at which the generator will check for a field at
    /// </summary>
    [DataField]
    public float FailsafeDistance = 16;

    /// <summary>
    ///     The prototype ID used to spawn a singularity.
    /// </summary>
    [DataField("spawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? SpawnPrototype = "Singularity";

    /// <summary>
    /// The masks the raycast should not go through
    /// </summary>
    [DataField]
    public int CollisionMask = (int)CollisionGroup.FullTileMask;

    /// <summary>
    /// Message to use when there's no containment field on cardinal directions
    /// </summary>
    [DataField]
    public LocId ContainmentFailsafeMessage;

    /// <summary>
    /// For how long the failsafe will cause the generator to stop working and not issue a failsafe warning
    /// </summary>
    [DataField]
    public TimeSpan FailsafeCooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long until the generator can issue a failsafe warning again
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextFailsafe;
}

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

using Content.Shared.Physics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, AutoGenerateComponentPause, NetworkedComponent, AutoGenerateComponentState]
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
    /// An inert generator will never be charged by particles, even if emagged.
    /// This is normally only used between activating and being destroyed, to avoid creating duplicate teslas.
    /// </summary>
    [DataField]
    public bool Inert;

    /// <summary>
    /// Allows the generator to ignore all the failsafe stuff, e.g. when emagged
    /// </summary>
    [DataField, AutoNetworkedField]
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
    public LocId ContainmentFailsafeMessage = "comp-generator-failsafe";

    /// <summary>
    /// For how long the failsafe will cause the generator to stop working and not issue a failsafe warning
    /// </summary>
    [DataField]
    public TimeSpan FailsafeCooldown = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long until the generator can issue a failsafe warning again
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextFailsafe = TimeSpan.Zero;
}

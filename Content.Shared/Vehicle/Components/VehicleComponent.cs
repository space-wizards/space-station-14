using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// This is particularly for vehicles that use
/// buckle. Stuff like clown cars may need a different
/// component at some point.
/// All vehicles should have Physics, Strap, and SharedPlayerInputMover components.
/// </summary>
[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedVehicleSystem))]
public sealed partial class VehicleComponent : Component
{
    /// <summary>
    /// The entity currently riding the vehicle.
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid? Rider;

    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid? LastRider;

    /// <summary>
    /// The base offset for the vehicle (when facing east)
    /// </summary>
    [ViewVariables]
    public Vector2 BaseBuckleOffset = Vector2.Zero;

    /// <summary>
    /// The sound that the horn makes
    /// </summary>
    [DataField("hornSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? HornSound = new SoundPathSpecifier("/Audio/Effects/Vehicle/carhorn.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f)
    };

    [ViewVariables]
    public EntityUid? HonkPlayingStream;

    /// Use ambient sound component for the idle sound.

    [DataField("hornAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? HornAction = "ActionVehicleHorn";

    /// <summary>
    /// The action for the horn (if any)
    /// </summary>
    [DataField("hornActionEntity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? HornActionEntity;

    /// <summary>
    /// Whether the vehicle has a key currently inside it or not.
    /// </summary>
    [DataField("hasKey")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool HasKey;

    /// <summary>
    /// Determines from which side the vehicle will be displayed on top of the player.
    /// </summary>

    [DataField("southOver")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool SouthOver;

    [DataField("northOver")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool NorthOver;

    [DataField("westOver")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool WestOver;

    [DataField("eastOver")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool EastOver;

    /// <summary>
    /// What the y buckle offset should be in north / south
    /// </summary>
    [DataField("northOverride")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float NorthOverride;

    /// <summary>
    /// What the y buckle offset should be in north / south
    /// </summary>
    [DataField("southOverride")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float SouthOverride;

    [DataField("autoAnimate")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AutoAnimate = true;

    [DataField("useHand")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool UseHand = true;

    [DataField("hideRider")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool HideRider;
}

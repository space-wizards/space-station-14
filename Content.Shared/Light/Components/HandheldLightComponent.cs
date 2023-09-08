using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Light.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedHandheldLightSystem))]
public sealed partial class HandheldLightComponent : Component
{
    public byte? Level;
    public bool Activated;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("wattage")]
    public float Wattage { get; set; } = .8f;

    [DataField("turnOnSound")]
    public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Items/flashlight_on.ogg");

    [DataField("turnOnFailSound")]
    public SoundSpecifier TurnOnFailSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField("turnOffSound")]
    public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/Items/flashlight_off.ogg");

    /// <summary>
    ///     Whether to automatically set item-prefixes when toggling the flashlight.
    /// </summary>
    /// <remarks>
    ///     Flashlights should probably be using explicit unshaded sprite, in-hand and clothing layers, this is
    ///     mostly here for backwards compatibility.
    /// </remarks>
    [DataField("addPrefix")]
    public bool AddPrefix = false;

    [DataField("toggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToggleAction = "ActionToggleLight";

    /// <summary>
    /// Whether or not the light can be toggled via standard interactions
    /// (alt verbs, using in hand, etc)
    /// </summary>
    [DataField("toggleOnInteract")]
    public bool ToggleOnInteract = true;

    [DataField("toggleActionEntity")]
    public EntityUid? ToggleActionEntity;

    public const int StatusLevels = 6;

    /// <summary>
    /// Specify the ID of the light behaviour to use when the state of the light is Dying
    /// </summary>
    [DataField("blinkingBehaviourId")]
    public string BlinkingBehaviourId { get; set; } = string.Empty;

    /// <summary>
    /// Specify the ID of the light behaviour to use when the state of the light is LowPower
    /// </summary>
    [DataField("radiatingBehaviourId")]
    public string RadiatingBehaviourId { get; set; } = string.Empty;

    [Serializable, NetSerializable]
    public sealed class HandheldLightComponentState : ComponentState
    {
        public byte? Charge { get; }

        public bool Activated { get; }

        public HandheldLightComponentState(bool activated, byte? charge)
        {
            Activated = activated;
            Charge = charge;
        }
    }
}

[Serializable, NetSerializable]
public enum HandheldLightVisuals
{
    Power
}

[Serializable, NetSerializable]
public enum HandheldLightPowerStates
{
    FullPower,
    LowPower,
    Dying,
}

using Content.Server.Light.EntitySystems;
using Content.Shared.Light.Component;
using Content.Shared.Sound;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Component that represents a powered handheld light source which can be toggled on and off.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(HandheldLightSystem))]
    public sealed class HandheldLightComponent : SharedHandheldLightComponent
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("wattage")] public float Wattage { get; set; } = .8f;

        /// <summary>
        ///     Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated { get; set; }

        [ViewVariables(VVAccess.ReadWrite)] [DataField("turnOnSound")] public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Items/flashlight_on.ogg");
        [ViewVariables(VVAccess.ReadWrite)] [DataField("turnOnFailSound")] public SoundSpecifier TurnOnFailSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
        [ViewVariables(VVAccess.ReadWrite)] [DataField("turnOffSound")] public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/Items/flashlight_off.ogg");

        /// <summary>
        ///     Client-side ItemStatus level
        /// </summary>
        public byte? LastLevel;
    }
}

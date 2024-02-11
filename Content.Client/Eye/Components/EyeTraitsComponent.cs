using Content.Client.Eye.Blinding;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Eye.Components
{
 [DataDefinition]
    public sealed partial class NightVision
    {
        [DataField("color", customTypeSerializer:typeof(ColorSerializer)), ViewVariables(VVAccess.ReadWrite)] public Color Color = Color.White;
        [DataField("range"), ViewVariables(VVAccess.ReadWrite)] public float Range = 0.5f;
        [DataField("power"), ViewVariables(VVAccess.ReadWrite)] public float Power = 0.2f;
        [DataField("minExposure"), ViewVariables(VVAccess.ReadWrite)] public float MinExposure = 2.0f;
        /// <summary>
        /// How much very bright lights bother this eye. 0.1 means we can handle very bright lighting.
        ///   2.0 means everything turns white
        /// </summary>
        [DataField("lightIntolerance"), ViewVariables(VVAccess.ReadWrite)] public float LightIntolerance = 0.5f;
    }

    [DataDefinition]
    public sealed partial class AutoExpose
    {
        [DataField("min"), ViewVariables(VVAccess.ReadWrite)]
        public float Min = 0.4f;
        [DataField("max"), ViewVariables(VVAccess.ReadWrite)]
        public float Max = 4.0f;            // 12 is a good limit for quite reasonable nightvision.
        [DataField("rampDown"), ViewVariables(VVAccess.ReadWrite)]
        public float RampDown = 0.2f;
        [DataField("rampDownNight"), ViewVariables(VVAccess.ReadWrite)]
        public float RampDownNight = 1.0f; // Lose night vision quite fast
        [DataField("rampUp"), ViewVariables(VVAccess.ReadWrite)]
        public float RampUp = 0.025f;
        [DataField("rampUpNight"), ViewVariables(VVAccess.ReadWrite)]
        public float RampUpNight = 0.0015f; // As the eyes start straining, how fast do you adjust? (exposure / sec)

        /// <summary>
        /// How bright you want the lights to appear in the centre of the screen when lights are bright
        /// </summary>
        [DataField("goalBrightness"), ViewVariables(VVAccess.ReadWrite)]
        public float GoalBrightness = 1.1f;

        /// <summary>
        /// How bright you want the lights to appear in the centre of the screen when lights are dim
        /// </summary>
        [DataField("goalBrightnessNight"), ViewVariables(VVAccess.ReadWrite)]
        public float GoalBrightnessNight = 0.60f;
    }

    [RegisterComponent]
    public sealed partial class EyeTraitsComponent : Component
    {
        // Characters natural traits
        [DataField("nightVision"), ViewVariables(VVAccess.ReadWrite)]
        public NightVision? Night { get; set; } = new();
        [DataField("autoExpose"), ViewVariables(VVAccess.ReadWrite)]
        public AutoExpose? AutoExpose { get; set; } = new();

        [DataField("reduction"), ViewVariables(VVAccess.ReadWrite)]
        public float Reduction = 1.0f; // If you put on sunglasses, increase this (and decrease exposure the same)

        public NightVision? CurrentNight => MaskProtection?.Night ?? EyeProtection?.Night ?? Night;
        public AutoExpose? CurrentAutoExpose => MaskProtection?.AutoExpose ?? EyeProtection?.AutoExpose ?? AutoExpose;

        // Current eye protection
        public EntityUid EyeProtectionUid = EntityUid.Invalid;
        public EyeProtectionComponent? EyeProtection = null;

        // Current Mask eye protection
        public EntityUid MaskProtectionUid = EntityUid.Invalid;
        public EyeProtectionComponent? MaskProtection = null;

        // If you want other stuff other than glasses to affect eyes, you'll need to combine them somehow?
    }
}

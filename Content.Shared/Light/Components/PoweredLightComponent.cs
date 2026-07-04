using Content.Shared.DeviceLinking;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Light.Components
{
    /// <summary>
    ///     Component that represents a wall light. It has a light bulb that can be replaced when broken.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause, Access(typeof(SharedPoweredLightSystem))]
    public sealed partial class PoweredLightComponent : Component
    {
        /*
         * Stop adding more fields, use components or I will shed you.
         */

        [DataField]
        public SoundSpecifier BurnHandSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

        [DataField]
        public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Machines/light_tube_on.ogg");

        // Should be using containerfill?
        [DataField]
        public EntProtoId? HasLampOnSpawn = null;

        [DataField("bulb")]
        public LightBulbType BulbType;

        [DataField, AutoNetworkedField]
        public bool On = true;

        [DataField]
        public bool IgnoreGhostsBoo;

        [DataField]
        public TimeSpan GhostBlinkingTime = TimeSpan.FromSeconds(10);

        [DataField]
        public TimeSpan GhostBlinkingCooldown = TimeSpan.FromSeconds(60);

        [ViewVariables]
        public ContainerSlot LightBulbContainer = default!;

        [AutoNetworkedField]
        public bool CurrentLit;

        [DataField, AutoNetworkedField, AutoPausedField]
        public TimeSpan LastThunk;

        [DataField, AutoPausedField]
        public TimeSpan? LastGhostBlink;

        [DataField]
        public ProtoId<SinkPortPrototype> OnPort = "On";

        [DataField]
        public ProtoId<SinkPortPrototype> OffPort = "Off";

        [DataField]
        public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

        /// <summary>
        /// How long it takes to eject a bulb from this
        /// </summary>
        [DataField]
        public float EjectBulbDelay = 2;

        /// <summary>
        /// Shock damage done to a mob that hits the light with an unarmed attack
        /// </summary>
        [DataField]
        public int UnarmedHitShock = 20;

        /// <summary>
        /// Stun duration applied to a mob that hits the light with an unarmed attack
        /// </summary>
        [DataField]
        public TimeSpan UnarmedHitStun = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Makes an entity with <see cref="PoweredLightComponent"/> toggle itself on and off.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
    public sealed partial class BlinkingPoweredLightComponent : Component
    {
        /// <summary>
        /// The time at which this component will remove itself, stopping the blinking.
        /// If null then this light will keep blinking forever.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        [AutoNetworkedField, AutoPausedField]
        public TimeSpan? StopBlinkingTime;
    }
}

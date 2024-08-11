using Content.Server.Light.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DeviceLinking;
using Content.Shared.Light.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Component that represents a wall light. It has a light bulb that can be replaced when broken.
    /// </summary>
    [RegisterComponent, Access(typeof(PoweredLightSystem))]
    public sealed partial class PoweredLightComponent : Component
    {
        [DataField("burnHandSound")]
        public SoundSpecifier BurnHandSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

        [DataField("turnOnSound")]
        public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Machines/light_tube_on.ogg");

        [DataField("hasLampOnSpawn", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? HasLampOnSpawn = null;

        [DataField("bulb")]
        public LightBulbType BulbType;

        [DataField("on")]
        public bool On = true;

        [DataField("ignoreGhostsBoo")]
        public bool IgnoreGhostsBoo;

        [DataField("ghostBlinkingTime")]
        public TimeSpan GhostBlinkingTime = TimeSpan.FromSeconds(10);

        [DataField("ghostBlinkingCooldown")]
        public TimeSpan GhostBlinkingCooldown = TimeSpan.FromSeconds(60);

        [ViewVariables]
        public ContainerSlot LightBulbContainer = default!;
        [ViewVariables]
        public bool CurrentLit;
        [ViewVariables]
        public bool IsBlinking;
        [ViewVariables]
        public TimeSpan LastThunk;
        [ViewVariables]
        public TimeSpan? LastGhostBlink;

        [DataField("onPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string OnPort = "On";

        [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string OffPort = "Off";

        [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string TogglePort = "Toggle";

        /// <summary>
        /// How long it takes to eject a bulb from this
        /// </summary>
        [DataField("ejectBulbDelay")]
        public float EjectBulbDelay = 2;

        /// <summary>
        /// Shock damage done to a mob that hits the light with an unarmed attack
        /// </summary>
        [DataField("unarmedHitShock")]
        public int UnarmedHitShock = 20;

        /// <summary>
        /// Stun duration applied to a mob that hits the light with an unarmed attack
        /// </summary>
        [DataField("unarmedHitStun")]
        public TimeSpan UnarmedHitStun = TimeSpan.FromSeconds(5);
    }
}

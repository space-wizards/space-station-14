using Content.Shared.Construction.Prototypes;
using Content.Shared.DeviceLinking;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Kitchen.Components
{
    [RegisterComponent]
    public sealed partial class MicrowaveComponent : Component
    {
        [DataField("cookTimeMultiplier"), ViewVariables(VVAccess.ReadWrite)]
        public float CookTimeMultiplier = 1;

        [DataField("baseHeatMultiplier"), ViewVariables(VVAccess.ReadWrite)]
        public float BaseHeatMultiplier = 100;

        [DataField("objectHeatMultiplier"), ViewVariables(VVAccess.ReadWrite)]
        public float ObjectHeatMultiplier = 100;

        [DataField("failureResult", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string BadRecipeEntityId = "FoodBadRecipe";

        #region  audio
        [DataField("beginCookingSound")]
        public SoundSpecifier StartCookingSound = new SoundPathSpecifier("/Audio/Machines/microwave_start_beep.ogg");

        [DataField("foodDoneSound")]
        public SoundSpecifier FoodDoneSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");

        [DataField("clickSound")]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [DataField("ItemBreakSound")]
        public SoundSpecifier ItemBreakSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

        public EntityUid? PlayingStream;

        [DataField("loopingSound")]
        public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/Machines/microwave_loop.ogg");
        #endregion

        [ViewVariables]
        public bool Broken;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public ProtoId<SinkPortPrototype> OnPort = "On";

        /// <summary>
        /// This is a fixed offset of 5.
        /// The cook times for all recipes should be divisible by 5,with a minimum of 1 second.
        /// For right now, I don't think any recipe cook time should be greater than 60 seconds.
        /// </summary>
        [DataField("currentCookTimerTime"), ViewVariables(VVAccess.ReadWrite)]
        public uint CurrentCookTimerTime = 0;

        /// <summary>
        /// Tracks the elapsed time of the current cook timer.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan CurrentCookTimeEnd = TimeSpan.Zero;

        /// <summary>
        /// The maximum number of seconds a microwave can be set to.
        /// This is currently only used for validation and the client does not check this.
        /// </summary>
        [DataField("maxCookTime"), ViewVariables(VVAccess.ReadWrite)]
        public uint MaxCookTime = 30;

        /// <summary>
        ///     The max temperature that this microwave can heat objects to.
        /// </summary>
        [DataField("temperatureUpperThreshold")]
        public float TemperatureUpperThreshold = 373.15f;

        public int CurrentCookTimeButtonIndex;

        public Container Storage = default!;

        [DataField]
        public string ContainerId = "microwave_entity_container";

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public int Capacity = 10;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public ProtoId<ItemSizePrototype> MaxItemSize = "Normal";

        /// <summary>
        /// How frequently the microwave can malfunction.
        /// </summary>
        [DataField]
        public float MalfunctionInterval = 1.0f;

        /// <summary>
        /// Chance of an explosion occurring when we microwave a metallic object
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float ExplosionChance = .1f;

        /// <summary>
        /// Chance of lightning occurring when we microwave a metallic object
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float LightningChance = .75f;

        /// <summary>
        /// If this microwave can give ids accesses without exploding
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool CanMicrowaveIdsSafely = true;
    }

    public sealed class BeingMicrowavedEvent : HandledEntityEventArgs
    {
        public EntityUid Microwave;
        public EntityUid? User;

        public BeingMicrowavedEvent(EntityUid microwave, EntityUid? user)
        {
            Microwave = microwave;
            User = user;
        }
    }
}

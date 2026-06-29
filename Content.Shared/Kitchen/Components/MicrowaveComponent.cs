using Content.Shared.DeviceLinking;
using Content.Shared.Item;
using Content.Shared.Kitchen.EntitySystems;
using Content.Shared.Temperature.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Kitchen.Components;

/// <summary>
///     A component applied to microwaves, which are used to heat entities/solutions
///     and produce microwave recipes.
/// </summary>
[RegisterComponent, Access(typeof(SharedMicrowaveSystem))]
[NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class MicrowaveComponent : Component
{
    /// <summary>
    ///     A multiplier applied to all microwave cooking timers.
    /// </summary>
    /// <remarks>
    ///     If you set a microwave for 30 seconds, but have a CookTimeMultiplier of 0.5,
    ///     recipes will be processed as if you're portioning for 30 seconds, but the
    ///     microwave's timer will take 15 seconds instead. So, if you have a recipe with
    ///     a cookTime of 10 seconds, you can still make that recipe 3 at a time.
    ///
    ///     Note that faster cooking times make the microwave less useful for actual heating
    ///     (temperature) as thermal energy added to the contents is based on elapsed time.
    /// </remarks>
    [DataField]
    public float CookTimeMultiplier = 1;

    /// <summary>
    ///     A multiplier for heat added to the contents of a microwave every frame.
    /// </summary>
    /// <remarks>
    ///     The formula is (frame time * BaseHeatMultiplier).
    ///     This is multiplied by <see cref="ObjectHeatMultiplier"/> when applied to entities
    ///     that have a <see cref="TemperatureComponent"/> (as opposed to solutions).
    /// </remarks>
    [DataField]
    public float BaseHeatMultiplier = 100;

    /// <summary>
    ///     A multiplier for added heat on entities with a <see cref="TemperatureComponent"/>.
    /// </summary>
    [DataField]
    public float ObjectHeatMultiplier = 100;

    /// <summary>
    ///     An entity that is produced when an item is melted in the microwave.
    /// </summary>
    /// <remarks>
    ///     The microwave will burn items that pass the <see cref="BurnWhenCookedWhitelist" />.
    /// </remarks>
    [DataField("failureResult", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public EntProtoId BadRecipeEntityId = "FoodBadRecipe";

    #region  audio
    /// <summary>
    ///     A sound that is played when the microwave is activated.
    /// </summary>
    [DataField("beginCookingSound")]
    public SoundSpecifier StartCookingSound = new SoundPathSpecifier("/Audio/Machines/microwave_start_beep.ogg");

    /// <summary>
    ///     A sound that is played when the microwave finishes.
    /// </summary>
    /// <remarks>
    ///     Beep... beep... beep
    /// </remarks>
    [DataField]
    public SoundSpecifier FoodDoneSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");

    /// <summary>
    ///     A sound that is played when a player navigates the microwave's UI - for example, selecting
    ///     a new cooking time.
    /// </summary>
    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    /// <summary>
    ///     An audio stream for the microwave's "cooking" hum.
    /// </summary>
    public EntityUid? PlayingStream;

    /// <summary>
    ///     The humming sound played when a microwave is actively cooking.
    /// </summary>
    [DataField]
    public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/Machines/microwave_loop.ogg");
    #endregion

    /// <summary>
    ///     Whether or not this microwave is broken.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool Broken;

    /// <summary>
    ///     A port used to activate the microwave via remote signal.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <summary>
    ///     This is a fixed offset of 5.
    ///     The cook times for all recipes should be divisible by 5, with a minimum of 1 second.
    /// </summary>
    /// <remarks>
    ///     For right now, I don't think any recipe cook time should be greater than 60 seconds.
    /// </remarks>
    [DataField, Access(typeof(SharedMicrowaveSystem), Other = AccessPermissions.ReadExecute)]
    [AutoNetworkedField]
    public uint CurrentCookTimerTime = 0;

    /// <summary>
    ///     The maximum number of seconds a microwave can be set to.
    ///     This is currently only used for validation and the client does not check this.
    /// </summary>
    [DataField]
    public uint MaxCookTime = 30;

    /// <summary>
    ///     The max temperature that this microwave can heat objects to.
    /// </summary>
    [DataField]
    public float TemperatureUpperThreshold = 373.15f;

    /// <summary>
    ///     The index of the currently-selected "cook time" button.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CurrentCookTimeButtonIndex;

    /// <summary>
    ///     The microwave's contents container.
    /// </summary>
    public Container Storage = default!;

    /// <summary>
    ///     The ID of the storage container for the microwave.
    /// </summary>
    [DataField]
    public string ContainerId = "microwave_entity_container";

    /// <summary>
    ///     How many items the microwave can hold.
    /// </summary>
    [DataField]
    public int Capacity = 10;

    /// <summary>
    ///     The largest item size that can fit in the microwave.
    /// </summary>
    [DataField]
    public ProtoId<ItemSizePrototype> MaxItemSize = "Normal";

    /// <summary>
    ///     How frequently the microwave can malfunction.
    /// </summary>
    [DataField]
    public TimeSpan MalfunctionInterval = TimeSpan.FromSeconds(1.0f);

    /// <summary>
    ///     Chance of an explosion occurring when we microwave a metallic object.
    /// </summary>
    /// <remarks>
    ///     This is rolled every <see cref="MalfunctionInterval"/>.
    /// </remarks>
    [DataField]
    public float ExplosionChance = .1f;

    /// <summary>
    ///     Chance of lightning occurring when we microwave a metallic object.
    /// </summary>
    /// <remarks>
    ///     This is rolled every <see cref="MalfunctionInterval"/>.
    /// </remarks>
    [DataField]
    public float LightningChance = .75f;

    /// <summary>
    ///     If this microwave can give ID cards new accesses without exploding.
    /// </summary>
    [DataField]
    public bool CanMicrowaveIdsSafely = true;

    /// <summary>
    ///     Entities that fulfill this whitelist will cause the microwave to malfunction
    ///     on activation. By default, this is metal objects.
    /// </summary>
    [DataField]
    public EntityWhitelist? MalfunctionWhenCookedWhitelist = new() { Tags = ["Metal"] };

    /// <summary>
    ///     Entities that fulfill this whitelist will create a burned mess when microwaved.
    ///     By default, this is plastic objects.
    /// </summary>
    [DataField]
    public EntityWhitelist? BurnWhenCookedWhitelist = new() { Tags = ["Plastic"] };

    /// <summary>
    ///     A "spark" entity spawned when this microwave malfunctions.
    /// </summary>
    [DataField]
    public EntProtoId MalfunctionSpark = "Spark";
}

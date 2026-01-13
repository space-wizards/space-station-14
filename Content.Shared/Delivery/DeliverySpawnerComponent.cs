using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
namespace Content.Shared.Delivery;

/// <summary>
/// Used to mark entities that are valid for spawning deliveries on.
/// If this requires power, it needs to be powered to count as a valid spawner.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeliverySpawnerComponent : Component
{
    /// <summary>
    /// The entity table to select deliveries from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// The max amount of deliveries this spawner can hold at a time.
    /// </summary>
    [DataField]
    public int MaxContainedDeliveryAmount = 20;

    /// <summary>
    /// The currently held amount of deliveries.
    /// They are stored as an int and only spawned on use, as to not create additional entities without the need to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ContainedDeliveryAmount;

    /// <summary>
    /// The sound to play when the spawner spawns a delivery.
    /// </summary>
    [DataField]
    public SoundSpecifier? SpawnSound = new SoundCollectionSpecifier("DeliverySpawnSounds", AudioParams.Default.WithVolume(-7));

    /// <summary>
    /// The sound to play when a spawner is opened, and spills all the deliveries out.
    /// </summary>
    [DataField]
    public SoundSpecifier? OpenSound = new SoundCollectionSpecifier("storageRustle");
}

using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Delivery;

/// <summary>
/// Used to mark entities that are valid for spawning deliveries on.
/// If this requires power, it needs to be powered to count as a valid spawner.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeliverySpawnerComponent : Component
{
    /// <summary>
    /// Whether this spawner is enabled.
    /// If false, it will not spawn any deliveries.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsEnabled = true;

    /// <summary>
    /// The entity table to select deliveries from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// The sound to play when the spawner spawns a delivery.
    /// </summary>
    [DataField]
    public SoundSpecifier? SpawnSound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg", AudioParams.Default.WithVolume(-7));

    /// <summary>
    /// The time before the spawning sound can play again.
    /// Meant to prevent sound spam when the spawner is actively spawning in deliveries.
    /// </summary>
    [DataField]
    public TimeSpan SpawnSoundCooldown = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The time at which the next sound is able to play.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextSoundTime = TimeSpan.Zero;
}

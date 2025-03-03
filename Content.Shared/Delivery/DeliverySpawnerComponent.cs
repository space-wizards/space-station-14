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
    /// The entity table to select deliveries from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    /// <summary>
    /// Whether this spawner is enabled.
    /// If false, it will not spawn any deliveries.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsEnabled = true;
}

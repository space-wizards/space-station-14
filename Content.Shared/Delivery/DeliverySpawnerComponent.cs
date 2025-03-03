using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Delivery;

/// <summary>
/// Component given to deliveries. I will write this eventually.
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

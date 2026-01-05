using Content.Shared.Botany.Items.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Items.Components;

/// <summary>
/// Anything that can be used to cross-pollinate plants.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BotanySwabSystem))]
public sealed partial class BotanySwabComponent : Component
{
    /// <summary>
    /// Delay between swab uses.
    /// </summary>
    [DataField]
    public TimeSpan SwabDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Name of a base plant prototype for the stored pollen snapshot.
    /// </summary>
    [DataField("plantId"), AutoNetworkedField]
    public EntProtoId? PlantProtoId;

    /// <summary>
    /// Serialized snapshot of plant components from the last swabbed plant.
    /// </summary>
    [DataField]
    public ComponentRegistry? PlantData;
}

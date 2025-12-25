using Robust.Shared.Prototypes;

namespace Content.Server.Botany.Components;

/// <summary>
/// Anything that can be used to cross-pollinate plants.
/// </summary>
[RegisterComponent]
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
    [DataField]
    public EntProtoId? PlantProtoId;

    /// <summary>
    /// Serialized snapshot of plant components from the last swabbed plant.
    /// </summary>
    [DataField]
    public ComponentRegistry? PlantData;
}

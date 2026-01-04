using Content.Server.Botany.Systems;

namespace Content.Server.Botany.Components;

/// <summary>
/// Component for using a hoe on a plant.
/// </summary>
[RegisterComponent]
[DataDefinition]
[Access(typeof(BotanyHoeSystem))]
public sealed partial class BotanyHoeComponent : Component
{
    /// <summary>
    /// How many weeds to remove when using the hoe.
    /// </summary>
    [DataField]
    public float WeedAmount = 5f;
}

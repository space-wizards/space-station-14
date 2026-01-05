using Content.Shared.Botany.Items.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Items.Components;

/// <summary>
/// Anything that can be used to use a hoe on a plant.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BotanyHoeSystem))]
public sealed partial class BotanyHoeComponent : Component
{
    /// <summary>
    /// How many weeds to remove when using the hoe.
    /// </summary>
    [DataField]
    public float WeedAmount = 5f;
}

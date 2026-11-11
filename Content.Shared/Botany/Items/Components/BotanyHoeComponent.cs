using Content.Shared.Botany.Items.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Items.Components;

/// <summary>
/// Component for items that can function as a hoe on plants.
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

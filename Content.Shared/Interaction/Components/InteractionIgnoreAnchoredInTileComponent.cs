using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

/// <summary>
/// Makes an entity always interactable regardless of other anchored entities on the same tile.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InteractionIgnoreAnchoredInTileComponent : Component;

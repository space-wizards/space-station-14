using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Requirements;

/// <summary>
/// Specifies the entity as requiring anchoring to keep the ActivatableUI open.
/// </summary>
[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class InteractionRequirementAnchorComponent : InteractionRequirementComponent;

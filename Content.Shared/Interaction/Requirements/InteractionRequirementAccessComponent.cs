using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Requirements;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent, Access(typeof(ActivatableUIRequiresAccessSystem))]
public sealed partial class InteractionRequirementAccessComponent : InteractionRequirementComponent;

using Content.Shared.Interaction;
using Content.Shared.PowerCell;
using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Requirements;

/// <summary>
/// Specifies that the attached entity requires <see cref="PowerCellDrawComponent"/> power.
/// </summary>
[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class InteractionRequirementPowerCellComponent : InteractionRequirementComponent;

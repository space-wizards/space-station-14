using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Components.SolutionManager;

/// <summary>
/// This is used to spawn solutions in a <see cref="SolutionContainerManagerComponent"/> when that entity is initialized.
/// We explicitly use a separate component for this to avoid YAML slop.
/// </summary>
[RegisterComponent]
public sealed partial class SolutionFillComponent : Component
{
    [DataField]
    public List<EntProtoId> Solutions;
}

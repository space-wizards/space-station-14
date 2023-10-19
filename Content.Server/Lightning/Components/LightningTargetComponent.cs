using Content.Shared.Lightning.Components;

namespace Content.Server.Lightning.Components;

/// <summary>
/// The component allows lightning to strike this target.
/// </summary>
[RegisterComponent]
public sealed partial class LightningTargetComponent : Component
{
    /// <summary>
    /// Priority level for selecting a lightning target. 
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Priority;
}

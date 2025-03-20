using Robust.Shared.Prototypes;

namespace Content.Shared.Farming;

[RegisterComponent]
public sealed partial class PloughToolComponent : Component
{
    /// <summary>
    /// Timer to complete ploughing action
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(8);
}
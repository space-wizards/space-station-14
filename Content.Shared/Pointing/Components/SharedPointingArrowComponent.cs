using Robust.Shared.GameStates;

namespace Content.Shared.Pointing.Components;

[NetworkedComponent]
public abstract partial class SharedPointingArrowComponent : Component
{
    /// <summary>
    /// When the pointing arrow ends
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("endTime")]
    public TimeSpan EndTime;
}

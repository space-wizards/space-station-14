using Robust.Shared.Serialization;

namespace Content.Shared.Sticky.Components;
using DrawDepth;

[RegisterComponent]
public sealed partial class StickyVisualizerComponent : Component
{
    /// <summary>
    ///     What sprite draw depth set when entity stuck.
    /// </summary>
    [DataField("stuckDrawDepth")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int StuckDrawDepth = (int) DrawDepth.Overdoors;

    /// <summary>
    ///     What sprite draw depth set when entity unstuck.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int DefaultDrawDepth;
}

[Serializable, NetSerializable]
public enum StickyVisuals : byte
{
    IsStuck
}

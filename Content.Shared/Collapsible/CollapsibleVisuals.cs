using Robust.Shared.Serialization;

namespace Content.Shared.Collapsible
{
    /// <summary>
    /// Stores the visuals for collapsible items
    /// </summary>
    [Serializable, NetSerializable]
    public enum CollapsibleVisuals : byte
    {
        IsCollapsed,
        InhandsVisible
    }
}

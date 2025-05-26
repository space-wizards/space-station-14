using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Event;

/// <summary>
/// Handles the screwing / cutting of a security gas mask and its impacts on the hailer
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SecHailerToolDoAfterEvent : SimpleDoAfterEvent
{
    public enum ToolQuality
    {
        Screwing,
        Cutting
    }
    public SecHailerToolDoAfterEvent(ToolQuality tool)
    {
        public ToolQuality UsedTool { get; private set; } = tool;
    }
}

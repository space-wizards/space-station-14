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

    public ToolQuality UsedTool { get; set; }

    public SecHailerToolDoAfterEvent(ToolQuality tool)
    {
        UsedTool = tool;
    }
}

using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Event;

/// <summary>
/// Handles the screwing / cutting of a security gas mask and its impacts on the hailer
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SecHailerToolDoAfterEvent : SimpleDoAfterEvent
{

    public ProtoId<ToolQualityPrototype> ToolQuality;

    public SecHailerToolDoAfterEvent(ProtoId<ToolQualityPrototype> quality)
    {
        ToolQuality = quality;
    }
}

using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Event;

/// <summary>
/// Handles the screwing / cutting of a security gas mask and its impacts on the hailer
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HailerToolDoAfterEvent : SimpleDoAfterEvent
{

    public ProtoId<ToolQualityPrototype> ToolQuality;

    public HailerToolDoAfterEvent(ProtoId<ToolQualityPrototype> quality)
    {
        ToolQuality = quality;
    }
}

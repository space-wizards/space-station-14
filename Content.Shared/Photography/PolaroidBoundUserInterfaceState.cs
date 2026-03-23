using Content.Shared.Paper;
using Robust.Shared.Serialization;

using static Content.Shared.Paper.PaperComponent;

namespace Content.Shared.Photography;

[Serializable, NetSerializable]
public sealed class PolaroidBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly byte[]? RawData;
    public readonly string CaptionText;
    public readonly PaperAction Mode;
    public readonly List<StampDisplayInfo> StampedBy;

    public PolaroidBoundUserInterfaceState(byte[]? rawData, string captionText, PaperAction mode, List<StampDisplayInfo> stampedBy)
    {
        RawData = rawData;
        CaptionText = captionText;
        Mode = mode;
        StampedBy = stampedBy;
    }
}

[Serializable, NetSerializable]
public enum PolaroidUiKey : byte
{
    Key
}

using System.Numerics;
using Content.Shared.Overlays;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.EntitySystems;

[Serializable, NetSerializable]
public readonly record struct AtmosDebugOverlayData(
        Vector2 Indices,
        float Temperature,
        float[]? Moles,
        AtmosDirection PressureDirection,
        AtmosDirection LastPressureDirection,
        AtmosDirection BlockDirection,
        int? InExcitedGroup,
        bool IsSpace,
        bool MapAtmosphere,
        bool NoGrid,
        bool Immutable);

[Serializable, NetSerializable]
public sealed class AtmosDebugOverlayMessage : DebugOverlayPayload
{
    public NetEntity GridId { get; }

    public Vector2i BaseIdx { get; }
    // LocalViewRange*LocalViewRange
    public AtmosDebugOverlayData?[] OverlayData { get; }

    public AtmosDebugOverlayMessage()
    {
        OverlayEnabled = false;
        GridId = default!;
        BaseIdx = default!;
        OverlayData = default!;
    }

    public AtmosDebugOverlayMessage(NetEntity gridIndices, Vector2i baseIdx, AtmosDebugOverlayData?[] overlayData)
    {
        OverlayEnabled = true;
        GridId = gridIndices;
        BaseIdx = baseIdx;
        OverlayData = overlayData;
    }
}

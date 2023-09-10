using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Fluids;

public abstract class SharedPuddleDebugOverlaySystem : EntitySystem
{
    protected const float LocalViewRange = 16;
    protected TimeSpan? NextTick = null;
    protected TimeSpan Cooldown = TimeSpan.FromSeconds(0.5f);
}

/// <summary>
/// Message for disable puddle overlay
/// </summary>
[Serializable, NetSerializable]
public sealed class PuddleOverlayDisableMessage : EntityEventArgs
{
}

/// <summary>
/// Message for puddle overlay display data
/// </summary>
[Serializable, NetSerializable]
public sealed class PuddleOverlayDebugMessage : EntityEventArgs
{
    public PuddleDebugOverlayData[] OverlayData { get; }

    public NetEntity GridUid { get; }


    public PuddleOverlayDebugMessage(NetEntity gridUid, PuddleDebugOverlayData[] overlayData)
    {
        GridUid = gridUid;
        OverlayData = overlayData;
    }
}

[Serializable, NetSerializable]
public readonly struct PuddleDebugOverlayData
{
    public readonly Vector2i Pos;
    public readonly FixedPoint2 CurrentVolume;

    public PuddleDebugOverlayData(Vector2i pos, FixedPoint2 currentVolume)
    {
        CurrentVolume = currentVolume;
        Pos = pos;
    }
}

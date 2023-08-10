using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Handles generic grid-drawing data, with zoom and dragging.
/// </summary>
public abstract class MapGridControl : Control
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    protected const float ScrollSensitivity = 8f;

    /// <summary>
    /// UI pixel radius.
    /// </summary>
    public const int UIDisplayRadius = 320;
    protected const int MinimapMargin = 4;

    protected float WorldMinRange;
    protected float WorldMaxRange;
    public float WorldRange;

    /// <summary>
    /// We'll lerp between the radarrange and actual range
    /// </summary>
    protected float ActualRadarRange;

    /// <summary>
    /// Controls the maximum distance that will display.
    /// </summary>
    public float MaxRadarRange { get; private set; } = 256f * 10f;

    public Vector2 MaxRadarRangeVector => new Vector2(MaxRadarRange, MaxRadarRange);

    protected Vector2 MidpointVector => new Vector2(MidPoint, MidPoint);

    protected int MidPoint => SizeFull / 2;
    protected int SizeFull => (int) ((UIDisplayRadius + MinimapMargin) * 2 * UIScale);
    protected int ScaledMinimapRadius => (int) (UIDisplayRadius * UIScale);
    protected float MinimapScale => WorldRange != 0 ? ScaledMinimapRadius / WorldRange : 0f;

    public event Action<float>? WorldRangeChanged;

    public MapGridControl(float minRange, float maxRange, float range)
    {
        IoCManager.InjectDependencies(this);
        SetSize = new Vector2(SizeFull, SizeFull);
        RectClipContent = true;
        MouseFilter = MouseFilterMode.Stop;
        ActualRadarRange = WorldRange;
        WorldMinRange = minRange;
        WorldMaxRange = maxRange;
        WorldRange = range;
        ActualRadarRange = range;
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        AddRadarRange(-args.Delta.Y * 1f / ScrollSensitivity * ActualRadarRange);
    }

    public void AddRadarRange(float value)
    {
        ActualRadarRange = Math.Clamp(ActualRadarRange + value, WorldMinRange, WorldMaxRange);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        if (!ActualRadarRange.Equals(WorldRange))
        {
            var diff = ActualRadarRange - WorldRange;
            const float lerpRate = 10f;

            WorldRange += (float) Math.Clamp(diff, -lerpRate * MathF.Abs(diff) * Timing.FrameTime.TotalSeconds, lerpRate * MathF.Abs(diff) * Timing.FrameTime.TotalSeconds);
            WorldRangeChanged?.Invoke(WorldRange);
        }
    }
}

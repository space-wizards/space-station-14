namespace Content.Shared.Radiation.Events;

/// <summary>
///     Raised on entity when it was irradiated
///     by some radiation source.
/// </summary>
public readonly record struct OnIrradiatedEvent(float FrameTime, float RadsPerSecond)
{
    public readonly float FrameTime = FrameTime;

    public readonly float RadsPerSecond = RadsPerSecond;

    public readonly EntityUid Origin;

    public float TotalRads => RadsPerSecond * FrameTime;

    public OnIrradiatedEvent(float frameTime, float radsPerSecond, EntityUid origin)
    {
        FrameTime = frameTime;
        RadsPerSecond = radsPerSecond;
        Origin = origin;
    }
}

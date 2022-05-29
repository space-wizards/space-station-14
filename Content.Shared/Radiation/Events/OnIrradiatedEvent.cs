namespace Content.Shared.Radiation.Events;

/// <summary>
///     Raised on entity when it was irradiated
///     by some radiation source.
/// </summary>
public sealed class OnIrradiatedEvent : EntityEventArgs
{
    public readonly float FrameTime;

    public readonly float RadsPerSecond;

    public float TotalRads => RadsPerSecond * FrameTime;

    public OnIrradiatedEvent(float frameTime, float radsPerSecond)
    {
        FrameTime = frameTime;
        RadsPerSecond = radsPerSecond;
    }
}

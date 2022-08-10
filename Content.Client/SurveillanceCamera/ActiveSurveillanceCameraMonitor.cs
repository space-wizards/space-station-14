namespace Content.Client.SurveillanceCamera;

[RegisterComponent]
public sealed class ActiveSurveillanceCameraMonitorVisualsComponent : Component
{
    public float TimeLeft = 30f;

    public Action? OnFinish;
}

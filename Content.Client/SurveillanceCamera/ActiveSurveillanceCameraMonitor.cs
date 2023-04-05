namespace Content.Client.SurveillanceCamera;

[RegisterComponent]
public sealed class ActiveSurveillanceCameraMonitorVisualsComponent : Component
{
    public float TimeLeft = 10f;

    public Action? OnFinish;
}

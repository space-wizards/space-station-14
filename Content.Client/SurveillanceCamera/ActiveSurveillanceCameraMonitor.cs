namespace Content.Client.SurveillanceCamera;

[RegisterComponent]
public class ActiveSurveillanceCameraMonitorVisualsComponent : Component
{
    public float TimeLeft = 30f;

    public Action? OnFinish;
}

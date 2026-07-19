namespace Content.Client.SurveillanceCamera;

[RegisterComponent]
public sealed partial class ActiveSurveillanceCameraMonitorVisualsComponent : Component
{
    public TimeSpan Deadline;

    public Action? OnFinish;
}

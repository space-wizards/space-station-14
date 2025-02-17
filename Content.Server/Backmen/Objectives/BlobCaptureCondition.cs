namespace Content.Server.Objectives.Conditions;

[RegisterComponent]
public sealed partial class BlobCaptureConditionComponent : Component
{
    [DataField("target")] public int Target { get; private set; } = 400;
}

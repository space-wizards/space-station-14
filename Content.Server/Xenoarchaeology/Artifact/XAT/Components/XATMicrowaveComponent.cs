namespace Content.Server.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact that is activated by being microwaved.
/// </summary>
[RegisterComponent, Access(typeof(XATMicrowaveSystem))]
public sealed partial class XATMicrowaveComponent : Component
{
}
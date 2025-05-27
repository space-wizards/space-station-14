using Robust.Shared.GameStates;

namespace Content.Shared.SurveillanceCamera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BodycamComponent: Component
{
    /// <summary>
    /// What state the bodycam is currently in.
    /// </summary>
    [DataField]
    public BodycamState State = BodycamState.Disabled;

    /// <summary>
    /// The name of the person wearing the bodycam.
    /// </summary>
    [DataField]
    public string Name = "Unknown";
}

public enum BodycamState
{
    Disabled,
    Active
}

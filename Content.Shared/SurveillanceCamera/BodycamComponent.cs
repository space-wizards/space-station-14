using Robust.Shared.GameStates;

namespace Content.Shared.SurveillanceCamera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BodycamComponent: Component
{
    /// <summary>
    /// What state the bodycam is currently in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public BodycamState State = BodycamState.Disabled;

    /// <summary>
    /// The name of the person wearing the bodycam.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Name = "Unknown";

    /// <summary>
    /// Who is currently wearing the bodycam.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Wearer = null;

    /// <summary>
    /// If the bodycam will glitch out when hit by an emp pulse.
    /// Keep in mind that this is redundant if the bodycam is powered by a cell as the cell will be drained.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool EmpVulnerable = true;
}

public enum BodycamState
{
    Disabled,
    Active
}

using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// This is used for using the "knock" spell when the artifact is activated
/// </summary>
[RegisterComponent, Access(typeof(XAEKnockSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XAEKnockComponent : Component
{
    /// <summary>
    /// The range of the spell
    /// </summary>
    [DataField, AutoNetworkedField]
    public float KnockRange = 4f;
}

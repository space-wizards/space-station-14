using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that activates when something dies nearby.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATDeathSystem)), AutoGenerateComponentState]
public sealed partial class XATDeathComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Range = 15;
}

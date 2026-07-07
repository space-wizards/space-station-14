using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact node that puts examine text on the artifact itself. Useful for flavor
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedXenoArtifactSystem)), AutoGenerateComponentState]
public sealed partial class XATExaminableTextComponent : Component
{
    /// <summary> Text to display. </summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId ExamineText;
}

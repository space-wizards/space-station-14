using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.DetailedInspect;

[RegisterComponent, NetworkedComponent]
public sealed partial class DetailedInspectComponent : Component
{
    [DataField]
    public LocId VerbText = "verbs-detailed-inspect";

    [DataField]
    public LocId VerbMessage = "verbs-detailed-inspect-message";

    [DataField(required: true)]
    public List<LocId> ExamineText;

    /// <summary>
    /// Whether or not the entries in ExamineText are separated by linebreaks and given ticks.
    /// </summary>
    [DataField]
    public bool Demarcated = false;

    /// <summary>
    /// Rooted directory of the icon for the verb.
    /// </summary>
    [DataField]
    public string Icon = "/Textures/Interface/VerbIcons/dot.svg.192dpi.png";
}

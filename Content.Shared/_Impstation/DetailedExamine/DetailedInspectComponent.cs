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
    /// Whether or not the entries in ExamineText are separated by linebreaks.
    /// </summary>
    [DataField]
    public bool LineBreak = false;

    /// <summary>
    /// Whether or not the entries in ExamineText are preceded by ticks. 
    /// </summary>
    [DataField]
    public bool TickEntries = false;

    /// <summary>
    /// Whether or not entries in the list are numbered.
    /// </summary>
    [DataField]
    public bool NumberedEntries = false;

    /// <summary>
    /// Rooted directory of the icon for the verb.
    /// </summary>
    [DataField]
    public string Icon = "/Textures/Interface/VerbIcons/dot.svg.192dpi.png";
}

using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Prying.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PryableComponent : Component
{
    [DataField]
    public ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

    /// <summary>
    /// Default time that the door should take to pry open.
    /// </summary>
    [DataField]
    public float PryTime = 1.5f;

    [DataField]
    public string VerbLocStr = "door-pry";
}

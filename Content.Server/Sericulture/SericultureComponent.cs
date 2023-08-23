namespace Content.Server.Sericulture;

[RegisterComponent]
public sealed partial class SericultureComponent : Component
{

    [DataField("popupText")]
    public string PopupText = "sericulture-failure-hunger";

    /// <summary>
    /// What will be produced at the end of the action.
    /// </summary>
    [DataField("entityProduced", required: true)]
    public string EntityProduced = "";

    [DataField("actionProto", required: true)]
    public string ActionProto = "";

    /// <summary>
    /// How long will it take to make.
    /// </summary>
    [DataField("productionLength", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float ProductionLength = 0;

    [DataField("hungerCost"), ViewVariables(VVAccess.ReadWrite)]
    public float HungerCost = 0f;
}

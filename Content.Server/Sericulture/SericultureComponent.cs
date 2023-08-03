namespace Content.Server.Sericulture;

[RegisterComponent]
public sealed class SericultureComponent : Component
{
    [DataField("hungerCost")]
    public float HungerCost = 0f;

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
    [DataField("productionLength", required: true)]
    public float ProductionLength = 0;
}

namespace Content.Server.AI.HTN.PrimitiveTasks;

/// <summary>
/// Chooses a nearby coordinate and puts it into the resulting key.
/// </summary>
public sealed class PickNearbyCoordinatesOperator : HTNOperator
{
    [ViewVariables, DataField("resultKey")]
    public string ResultKey = "MovementTarget";
}

namespace Content.Server.Maps;

[ImplicitDataDefinitionForInheritors]
public abstract class GameMapCondition
{
    [DataField("inverted")]
    public bool Inverted { get; private set; }
    public abstract bool Check(GameMapPrototype map);
}

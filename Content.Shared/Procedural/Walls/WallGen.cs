namespace Content.Shared.Procedural.Walls;

[ImplicitDataDefinitionForInheritors]
public abstract class WallGen
{
    [DataField("rules")]
    public virtual List<IWallRuleGen> Rules { get; } = new();
}

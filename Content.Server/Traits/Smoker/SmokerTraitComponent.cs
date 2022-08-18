namespace Content.Server.Traits.Smoker;

[RegisterComponent]
[Access(typeof(SmokerTraitSystem))]
public sealed class SmokerTraitComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float CurrentCraving;

    public CravingThreshold CurrentThreshold;
}

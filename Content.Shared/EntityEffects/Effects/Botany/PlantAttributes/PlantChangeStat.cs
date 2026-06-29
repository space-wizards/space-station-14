namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantChangeStat : EntityEffectBase<PlantChangeStat>
{
    /// <remarks>
    /// This is the worst thing in the code base.
    /// It's meant to be generic and expandable I guess? But it's looking for a specific datafield and then
    /// sending it into an if else if else if statement that filters by object type and randomly flips bits.
    /// </remarks>
    [DataField (required: true)]
    public string TargetValue = string.Empty;

    [DataField]
    public float MinValue;

    [DataField]
    public float MaxValue;

    [DataField]
    public int Steps;
}

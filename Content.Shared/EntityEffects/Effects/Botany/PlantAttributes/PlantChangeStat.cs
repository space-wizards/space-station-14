namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantChangeStat : EntityEffect
{
    /// <summary>
    /// The property of the plant we target (by reflecting on its type)
    /// </summary>
    /// <remarks>
    /// This is the worst thing in the code base.
    /// It's meant to be generic and expandable I guess? But it's looking for a specific datafield and then
    /// sending it into an if else if else if statement that filters by object type and randomly flips bits.
    /// </remarks>
    [DataField(required: true)]
    public string TargetValue = string.Empty;

    /// <summary>
    /// The minimum value a float value can assume.
    /// </summary>
    [DataField]
    public float MinValue;

    /// <summary>
    /// The maximum value a float value can assume.
    /// </summary>
    [DataField]
    public float MaxValue;

    /// <summary>
    /// How often to mutate a float field.
    /// See PlantChangeStatEntityEffectSystem.MutateFloat in Content.Server.
    /// </summary>
    [DataField]
    public int Steps;
}

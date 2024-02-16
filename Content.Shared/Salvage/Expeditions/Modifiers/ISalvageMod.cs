namespace Content.Shared.Salvage.Expeditions.Modifiers;

public interface ISalvageMod
{
    /// <summary>
    /// Player-friendly version describing this modifier.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Cost for difficulty modifiers.
    /// </summary>
    float Cost { get; }
}

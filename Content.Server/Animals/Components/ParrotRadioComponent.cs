namespace Content.Server.Animals.Components;

/// <summary>
/// Makes an entity able to learn things through an equipped radios and parrot things through radios
/// </summary>
[RegisterComponent]
public sealed partial class ParrotRadioComponent : Component
{
    /// <summary>
    /// Odds of the parrot attempting to speak on the radio.
    /// </summary>
    [DataField]
    public float RadioAttemptChance = 0.3f;
}

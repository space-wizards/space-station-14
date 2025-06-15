namespace Content.Server.Animals.Components;

/// <summary>
/// Makes an entity able to listen to messages from IC chat and attempt to commit them to memory
/// </summary>
[RegisterComponent]
public sealed partial class ParrotListenerComponent : Component
{
    /// <summary>
    /// Whether this entity ignores entities with ParrotSpeakerComponents.
    ///
    /// This is optional in case parrots are close to each other, or parrots learn via radio from other parrots.
    /// This may lead to samey voice lines, and for parrots with accents, this can quickly devolve from
    /// SQUAWK! Polly wants a cracker! BRAAWK
    /// to
    /// BRAAWK! SQUAWK! RAWWK! Polly wants a cracker! AAWK! AWWK! Cracker! SQUAWK! BRAWWK! SQUAWK!
    /// This is limited by the message length limit on ParrotMemoryComponent, but can be prevented entirely here
    /// </summary>
    [DataField]
    public bool IgnoreParrotSpeakers;
}

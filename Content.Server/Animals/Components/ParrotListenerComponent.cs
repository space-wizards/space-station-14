using Content.Shared.Whitelist;

namespace Content.Server.Animals.Components;

/// <summary>
/// Makes an entity able to listen to messages from IC chat and attempt to commit them to memory
/// </summary>
[RegisterComponent]
public sealed partial class ParrotListenerComponent : Component
{
    /// <summary>
    /// Whitelist for purposes of limiting which entities a parrot will listen to
    ///
    /// This is here because parrots can learn via local chat or radio from other parrots. this can quickly devolve from
    /// SQUAWK! Polly wants a cracker! BRAAWK
    /// to
    /// BRAAWK! SQUAWK! RAWWK! Polly wants a cracker! AAWK! AWWK! Cracker! SQUAWK! BRAWWK! SQUAWK!
    /// This is limited somewhat by the message length limit on ParrotMemoryComponent, but can be prevented entirely here
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist for purposes of ignoring entities
    /// As above, this is here to force parrots to ignore certain entities.
    /// For example, polly will be consistently mapped around EngiDrobes, which will consistently say stuff like
    /// "Guaranteed to protect your feet from industrial accidents!"
    /// If polly ends up constantly advertising engineering drip, this can be used to prevent it.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}

using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech.Components;

/// <summary>
/// Suppresses emotes with the given categories or ID.
/// Additionally, if the Scream Emote would be blocked, also blocks the Scream Action.
/// </summary>
[RegisterComponent]
public sealed partial class EmoteBlockerComponent : Component
{
    /// <summary>
    /// Which categories of emotes are blocked by this component.
    /// </summary>
    [DataField]
    public HashSet<EmoteCategory> BlocksCategories = [];

    /// <summary>
    /// IDs of which specific emotes are blocked by this component.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<EmotePrototype>> BlocksEmotes = [];
}

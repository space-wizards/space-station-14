using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Components;

/// <summary>
/// Suppresses emotes with the given categories or ID.
/// Additionally, if the Scream Emote would be blocked, also blocks the Scream Action.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmoteBlockerComponent : Component
{
    /// <summary>
    /// Which categories of emotes are blocked by this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EmoteCategory> BlocksCategories = [];

    /// <summary>
    /// IDs of which specific emotes are blocked by this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<EmotePrototype>> BlocksEmotes = [];
}

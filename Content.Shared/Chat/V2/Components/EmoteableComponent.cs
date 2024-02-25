using Robust.Shared.GameStates;

namespace Content.Shared.Chat.V2.Components;

/// <summary>
/// Declares that this entity can whisper.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmoteableComponent : Component
{
    // TODO: Emotes shouldn't work off audible-like "range"

    /// <summary>
    /// How far can this emote be seen from?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 10.0f;
}

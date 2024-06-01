using Robust.Shared.GameStates;

namespace Content.Shared.Chat.V2.Components;

/// <summary>
/// Declares that this entity can emote.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanVisualChatComponent : Component
{
    /// <summary>
    /// How far can this emote be seen from?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 10.0f;
}

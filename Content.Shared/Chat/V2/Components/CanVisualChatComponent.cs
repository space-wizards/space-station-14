using Robust.Shared.GameStates;

namespace Content.Shared.Chat.V2.Components;

/// <summary>
/// Declares that this entity can emote.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CanVisualChatComponent : Component;

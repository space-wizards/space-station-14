using Robust.Shared.GameStates;

namespace Content.Shared.Chat.V2.Components;

/// <summary>
/// Declares that this entity can chat in local chat
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CanVerbalChatComponent : Component;

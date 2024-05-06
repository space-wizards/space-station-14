using Robust.Shared.GameStates;

namespace Content.Shared.Chat.V2.Components;

/// <summary>
/// Declares that this entity can chat in local chat
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanLocalChatComponent : Component
{
    // TODO: Ensure you can't locally chat in insufficient atmosphere

    /// <summary>
    /// How far can this entity be heard from?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 10.0f;
}

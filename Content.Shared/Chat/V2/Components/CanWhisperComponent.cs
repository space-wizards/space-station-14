using Robust.Shared.GameStates;

namespace Content.Shared.Chat.V2.Components;

/// <summary>
/// Declares that this entity can whisper.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanWhisperComponent : Component
{
    // TODO: Ensure you can't whisper in insufficient atmosphere

    /// <summary>
    /// How far can this entity be clearly heard from?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinRange = 2.0f;

    /// <summary>
    /// How far can this entity be heard at all from?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxRange = 5.0f;
}

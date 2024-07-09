using Robust.Shared.GameStates;

namespace Content.Shared.Chat.V2.Components;

/// <summary>
/// Declares that this entity uses a removable piece of equipment (e.g. a headset) to send messages on the radio.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanRadioUsingEquipmentComponent : Component
{
    /// <summary>
    /// What channels can this entity talk on?
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Channels = new();
}

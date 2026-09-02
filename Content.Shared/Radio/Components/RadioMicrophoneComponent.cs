using Content.Shared.Radio.EntitySystems;
using Content.Shared.Chat;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.Components;

/// <summary>
/// Listens for local chat messages and relays them to some radio frequency.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRadioDeviceSystem))]
public sealed partial class RadioMicrophoneComponent : Component
{
    /// <summary>
    /// Radio channel on which local speech is broadcast.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<RadioChannelPrototype> BroadcastChannel = SharedChatSystem.CommonChannel;

    /// <summary>
    /// Maximum distance from the microphone at which speech is heard.
    /// </summary>
    [DataField]
    public int ListenRange = 4;

    /// <summary>
    /// Whether the microphone is currently broadcasting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// Whether the microphone requires power to operate.
    /// </summary>
    [DataField]
    public bool PowerRequired;

    /// <summary>
    /// Whether interacting with this entity toggles it on/off, or not.
    /// </summary>
    [DataField]
    public bool ToggleOnInteract = true;

    /// <summary>
    /// Whether the speaker must have an unobstructed path to the radio to speak, or now.
    /// </summary>
    [DataField]
    public bool UnobstructedRequired;
}

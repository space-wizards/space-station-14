using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.Components;

/// <summary>
/// Handles intercom ui and is authoritative on the channels an intercom can access.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class IntercomComponent : Component
{
    /// <summary>
    /// Does this intercom require power to function
    /// </summary>
    [DataField]
    public bool RequiresPower = true;

    [DataField, AutoNetworkedField]
    public bool SpeakerEnabled;

    [DataField, AutoNetworkedField]
    public bool MicrophoneEnabled;

    [DataField, AutoNetworkedField]
    public ProtoId<RadioChannelPrototype>? CurrentChannel;

    /// <summary>
    /// The list of radio channel prototypes this intercom can choose between.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<RadioChannelPrototype>> SupportedChannels = new();
}

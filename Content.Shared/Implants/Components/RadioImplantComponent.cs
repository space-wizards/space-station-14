using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants.Components;

/// <summary>
/// Gives the user access to a given channel without the need for a headset.
/// </summary>
[RegisterComponent]
public sealed partial class RadioImplantComponent : Component
{
    /// <summary>
    /// The radio channel(s) to grant access to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<RadioChannelPrototype>[] RadioChannels;

    /// <summary>
    /// The radio channels that have been added by the implant to a user.
    /// Used to track which channels were successfully added (not already in user)
    /// </summary>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> AddedChannels = new() {};
}

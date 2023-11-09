using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.Components;

/// <summary>
///     This component is required to receive radio message events.
/// </summary>
[RegisterComponent, Access(typeof(RadioSystem))]
public sealed partial class ActiveRadioComponent : Component
{
    /// <summary>
    ///     Channels to load the frequencies of.
    /// </summary>
    /// <remarks>
    ///     Not used in logic, just when initializing.
    /// </remarks>
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();

    /// <summary>
    ///     The channel frequencies that this radio is listening on.
    /// </summary>
    [DataField]
    public HashSet<int> Frequencies = new();

    /// <summary>
    ///     If this radio can hear all messages on all maps
    /// </summary>
    [DataField("globalReceive")]
    public bool GlobalReceive = false;
}

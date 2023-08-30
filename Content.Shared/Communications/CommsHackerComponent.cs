using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Communications;

/// <summary>
/// Component for hacking a communications console to call in a threat.
/// Can only be done once, the component is remove afterwards.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCommsHackerSystem))]
public sealed partial class CommsHackerComponent : Component
{
    /// <summary>
    /// Time taken to hack the console
    /// </summary>
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Possible threats to choose from.
    /// </summary>
    [DataField("threats", required: true)]
    public List<Threat> Threats = new();
}

/// <summary>
/// A threat that can be called in to the station by a ninja hacking a communications console.
/// Generally some kind of mid-round minor antag, though you could make it call in scrubber backflow if you wanted to.
/// You wouldn't do that, right?
/// </summary>
[DataDefinition]
public sealed partial class Threat
{
    /// <summary>
    /// Locale id for the announcement to be made from CentCom.
    /// </summary>
    [DataField("announcement")]
    public string Announcement = default!;

    /// <summary>
    /// The game rule for the threat to be added, it should be able to work when added mid-round otherwise this will do nothing.
    /// </summary>
    [DataField("rule", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Rule = default!;
}

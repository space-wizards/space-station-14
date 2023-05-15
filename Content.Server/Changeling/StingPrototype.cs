using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Changeling;

/// <summary>
/// Data for a sting.
/// Alt-click a mob to try and sting it.
/// May use some of your chemicals.
/// </summary>
[Prototype("sting")]
public sealed class StingPrototype : IPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// How many chemicals are required to use this sting, if any.
    /// </summary>
    [DataField("cost"), ViewVariables(VVAccess.ReadWrite)]
    public int Cost = 0;

    /// <summary>
    /// How long you have to wait after stinging before stinging again.
    /// </summary>
    [DataField("delay"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The event to raise on the changeling using the sting.
    /// Cancelling it will prevent using chemicals.
    /// </summary>
    [DataField("event", required: true)]
    public StingEvent? Event;
}

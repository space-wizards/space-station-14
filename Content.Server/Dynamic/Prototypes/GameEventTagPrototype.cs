using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Dynamic.Prototypes;

/// <summary>
///     Used to tag game events in order to weight them differently
///     etc. in storytellers.
/// </summary>
[Prototype("gameEventTag")]
public class GameEventTagPrototype : IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = default!;
}

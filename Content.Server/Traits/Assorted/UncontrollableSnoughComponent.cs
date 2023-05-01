using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This is used for the occasional sneeze or cough.
/// </summary>
[RegisterComponent]
public sealed class UncontrollableSnoughComponent : Component
{
    /// <summary>
    /// Emote to play when snoughing
    /// </summary>
    [DataField("emote", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string EmoteId = String.Empty;

    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    [DataField("timeBetweenIncidents", required: true)]
    public Vector2 TimeBetweenIncidents { get; }

    public float NextIncidentTime;
}

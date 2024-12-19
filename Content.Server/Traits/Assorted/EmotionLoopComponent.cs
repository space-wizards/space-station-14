namespace Content.Server.Traits.Assorted;

using System.Numerics;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

/// <summary>
/// This component allows triggering any emotion at random intervals.
/// </summary>
[RegisterComponent, Access(typeof(EmotionLoopSystem))]
public sealed partial class EmotionLoopComponent : Component
{
    /// <summary>
    /// A minimum interval between emotions.
    /// </summary>
    [DataField("minTimeBetweenEmotions", required: true)]
    public TimeSpan MinTimeBetweenEmotions { get; private set; }

    /// <summary>
    /// A maximum interval between emotions.
    /// </summary>
    [DataField("maxTimeBetweenEmotions", required: true)]
    public TimeSpan MaxTimeBetweenEmotions { get; private set; }

    /// <summary> 
    /// A set of emotes that will be randomly picked from. 
    /// <see cref="EmotePrototype"/> 
    /// </summary> 
    [DataField("emotes", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<EmotePrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public HashSet<string> Emotes = new();

    public TimeSpan NextIncidentTime;
}

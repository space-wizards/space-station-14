using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Mobs;

/// <summary>
///     Mobs with this component will emote when they become unconscious.
/// </summary>
[RegisterComponent]
public sealed partial class UnconsciousComponent : Component
{
    /// <summary>
    ///     The emote prototype to use.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string Prototype = "DefaultUnconscious";
}
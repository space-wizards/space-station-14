using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Mobs;

/// <summary>
///     Mobs with this component will emote a deathgasp when they die.
/// </summary>
/// <see cref="DeathgaspSystem"/>
[RegisterComponent]
public sealed partial class DeathgaspComponent : Component
{
    /// <summary>
    ///     The emote prototype to use.
    /// </summary>
    [DataField("prototype", customTypeSerializer:typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string Prototype = "DefaultDeathgasp";
}

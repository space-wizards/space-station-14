using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

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
    [DataField]
    public ProtoId<EmotePrototype> Prototype = "DefaultDeathgasp";
}

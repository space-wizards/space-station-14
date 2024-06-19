using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.GreyStation.Hailer;

[RegisterComponent, Access(typeof(SharedDeathgaspMaskSystem))]
public sealed partial class DeathgaspMaskComponent : Component
{
    /// <summary>
    /// Emote prototype to replace the wearer's deathgasp with.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EmotePrototype> Prototype = string.Empty;

    /// <summary>
    /// Previous emote prototype to revert back to when pulled down.
    /// </summary>
    [DataField]
    public ProtoId<EmotePrototype>? PreviousPrototype;
}

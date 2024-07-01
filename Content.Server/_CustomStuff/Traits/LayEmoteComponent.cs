using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._CustomStuff.Traits;

/// <summary>
/// Allows players to lay down and stand up using emotes.
/// </summary>
[RegisterComponent, Access(typeof(LayEmoteSystem))]
public sealed partial class LayEmoteComponent : Component
{
    [DataField("layEmote", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string LayEmoteId = "LayDown";

    [DataField("standEmote", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string StandEmoteId = "StandUp";

    [ViewVariables]
    public bool Laying = false;
}

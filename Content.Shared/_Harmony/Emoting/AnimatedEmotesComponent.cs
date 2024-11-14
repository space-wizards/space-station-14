// Original code by whateverusername0 from Goob-Station at commit 3022db4
// Available at: https://github.com/Goob-Station/Goob-Station/blob/3022db48e89ff00b762004767e7850023df3ee97/Content.Shared/_Goobstation/Emoting/AnimatedEmotesComponent.cs
// Rewritten by Jajsha to remove duplicate code.

using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Harmony.Emoting;

// use as a template
//[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationNameEmoteEvent : EntityEventArgs { }

[Serializable, NetSerializable, DataDefinition]
public sealed partial class AnimatedEmoteEvent : EntityEventArgs
{
    // Name of the emote to play
    [DataField] public string Emote;

    //Length of the emote, in miliseconds
    [DataField] public int Length;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class AnimatedEmotesComponent : Component
{
    [DataField] public ProtoId<EmotePrototype>? Emote;
}

[Serializable, NetSerializable]
public sealed partial class AnimatedEmotesComponentState : ComponentState
{
    public ProtoId<EmotePrototype>? Emote;

    public AnimatedEmotesComponentState(ProtoId<EmotePrototype>? emote)
    {
        Emote = emote;
    }
}

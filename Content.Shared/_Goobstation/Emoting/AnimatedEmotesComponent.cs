using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Emoting;

// use as a template
//[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationNameEmoteEvent : EntityEventArgs { }

[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationFlipEmoteEvent : EntityEventArgs { }
[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationSpinEmoteEvent : EntityEventArgs { }
[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationJumpEmoteEvent : EntityEventArgs { }

[RegisterComponent, NetworkedComponent] public sealed partial class AnimatedEmotesComponent : Component
{
    [DataField] public ProtoId<EmotePrototype>? Emote;
}

[Serializable, NetSerializable] public sealed partial class AnimatedEmotesComponentState : ComponentState
{
    public ProtoId<EmotePrototype>? Emote;

    public AnimatedEmotesComponentState(ProtoId<EmotePrototype>? emote)
    {
        Emote = emote;
    }
}

using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Dummy
{
    [RegisterComponent, NetworkedComponent]
    public sealed class DummyComponent : Component
    {
        [DataField("enabled")]
        public bool Enabled = false;

        [DataField("teleportDummyAction")]
        public InstantAction TeleportDummyAction = new()
        {
            UseDelay = TimeSpan.FromSeconds(30),
            Icon = new SpriteSpecifier.Texture(new("Structures/Walls/solid.rsi/full.png")),
            DisplayName = "mime-invisible-wall",
            Description = "mime-invisible-wall-desc",
            Priority = -1,
            Event = new TeleportDummyActionEvent(),
        };
    }
}

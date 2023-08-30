using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Tools.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class MultipleToolComponent : Component
    {
        [DataDefinition]
        public sealed partial class ToolEntry
        {
            [DataField("behavior", required: true)]
            public PrototypeFlags<ToolQualityPrototype> Behavior = new();

            [DataField("useSound")]
            public SoundSpecifier? Sound;

            [DataField("changeSound")]
            public SoundSpecifier? ChangeSound;

            [DataField("sprite")]
            public SpriteSpecifier? Sprite;
        }

        [DataField("entries", required: true)]
        public ToolEntry[] Entries { get; private set; } = Array.Empty<ToolEntry>();

        [ViewVariables]
        public uint CurrentEntry = 0;

        [ViewVariables]
        public string CurrentQualityName = string.Empty;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool UiUpdateNeeded;

        [DataField("statusShowBehavior")]
        public bool StatusShowBehavior = true;
    }

    [NetSerializable, Serializable]
    public sealed class MultipleToolComponentState : ComponentState
    {
        public readonly uint Selected;

        public MultipleToolComponentState(uint selected)
        {
            Selected = selected;
        }
    }
}

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Tools.Components
{
    [NetworkedComponent]
    public abstract class SharedMultipleToolComponent : Component
    {
        [DataDefinition]
        public sealed class ToolEntry
        {
            [DataField("behavior", required: true)]
            public PrototypeFlags<ToolQualityPrototype> Behavior { get; } = new();

            [DataField("useSound")]
            public SoundSpecifier? Sound { get; } = null;

            [DataField("changeSound")]
            public SoundSpecifier? ChangeSound { get; } = null;

            [DataField("sprite")]
            public SpriteSpecifier? Sprite { get; } = null;
        }

        [DataField("entries", required: true)]
        public ToolEntry[] Entries { get; } = Array.Empty<ToolEntry>();

        [ViewVariables]
        public uint CurrentEntry = 0;

        [ViewVariables]
        public string CurrentQualityName = String.Empty;
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

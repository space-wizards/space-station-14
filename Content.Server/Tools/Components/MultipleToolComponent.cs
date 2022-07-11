using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Server.Tools.Components
{
    /// <summary>
    ///     Not to be confused with Multitool (power)
    /// </summary>
    [RegisterComponent]
    public sealed class MultipleToolComponent : SharedMultipleToolComponent
    {
        [DataDefinition]
        public sealed class ToolEntry
        {
            [DataField("behavior", required:true)]
            public PrototypeFlags<ToolQualityPrototype> Behavior { get; } = new();

            [DataField("useSound")]
            public SoundSpecifier? Sound { get; } = null;

            [DataField("changeSound")]
            public SoundSpecifier? ChangeSound { get; } = null;

            [DataField("sprite")]
            public SpriteSpecifier? Sprite { get; } = null;
        }

        [DataField("entries", required:true)]
        public ToolEntry[] Entries { get; } = Array.Empty<ToolEntry>();

        [ViewVariables]
        public int CurrentEntry = 0;

        [ViewVariables]
        public string CurrentQualityName = string.Empty;
    }
}

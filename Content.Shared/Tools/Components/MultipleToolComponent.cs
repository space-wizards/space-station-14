using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Tools.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class MultipleToolComponent : Component
{
    [DataDefinition]
    public sealed partial class ToolEntry
    {
        [DataField(required: true)]
        public PrototypeFlags<ToolQualityPrototype> Behavior = new();

        [DataField]
        public SoundSpecifier? UseSound;

        [DataField]
        public SoundSpecifier? ChangeSound;

        [DataField]
        public SpriteSpecifier? Sprite;
    }

    [DataField(required: true)]
    public ToolEntry[] Entries { get; private set; } = Array.Empty<ToolEntry>();

    [ViewVariables]
    [AutoNetworkedField]
    public uint CurrentEntry = 0;

    [ViewVariables]
    public string CurrentQualityName = string.Empty;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool UiUpdateNeeded;

    [DataField]
    public bool StatusShowBehavior = true;
}

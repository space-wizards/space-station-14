using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.ChangeAlertLevel
{
    public partial class SharedChangeAlertLevelComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public string AlertLevelOnActivate = default!;

        [ViewVariables(VVAccess.ReadWrite), DataField(required: true)]
        public string TextField = default!;

        [ViewVariables(VVAccess.ReadWrite), DataField]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
    }
}

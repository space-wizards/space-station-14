using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.ChangeAlertLevel
{
    [RegisterComponent]
    public partial class AlertLevelOnPressComponent : Component
    {
        [DataField(required: true)]
        public string AlertLevelOnActivate = default!;

        [DataField(required: true)]
        public string TextField = default!;

        [DataField]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
    }
}

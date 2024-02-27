using Content.Shared.ChangeAlertLevel;
using Robust.Shared.Audio;

namespace Content.Server.ChangeAlertLevel
{
    /// <summary>
    ///     Sets an alert level when activated
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ChangeAlertLevelSystem))]
    public sealed partial class ChangeAlertLevelComponent : SharedChangeAlertLevelComponent
    {
        [DataField("alertLevelOnActivate")] public string AlertLevelOnActivate = default!;
        [DataField("textField")] public string TextField = default!;
        [DataField("clickSound")]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
    }
}

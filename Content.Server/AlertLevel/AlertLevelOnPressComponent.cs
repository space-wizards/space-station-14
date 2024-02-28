using Content.Shared.AlertLevelOnPress;
using Robust.Shared.Audio;

namespace Content.Server.AlertLevelOnPress;

[RegisterComponent]
[Access(typeof(AlertLevelOnPressSystem))]
public sealed partial class AlertLevelOnPressComponent : SharedAlertLevelOnPressComponent
{
    [DataField(required: true)]
    public string AlertLevelOnActivate = default!;

    [DataField(required: true)]
    public string TextField = default!;

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}

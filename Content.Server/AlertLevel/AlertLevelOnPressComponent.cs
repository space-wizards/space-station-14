using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using System.ComponentModel;
using Content.Shared.AlertLevelOnPress;

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

// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.DeadSpace.Renegade.Components;

[RegisterComponent]
public sealed partial class RenegadeEswordComponent : Component
{
    [DataField]
    public bool IsConnected = false;

    [DataField]
    public EntityUid? SwordOwner;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg")
    {
        Params = AudioParams.Default.WithVolume(5f)
    };
}

public partial class RecallRenegadeEswordEvent : InstantActionEvent { }

public partial class RenegadeEswordTeleport : InstantActionEvent { }

// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Implants.Revive.Components;

[RegisterComponent]
public sealed partial class ReviveImplantComponent : Component
{
    [DataField]
    public float InjectTime = 4f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public SoundSpecifier ImplantedSound = new SoundPathSpecifier("/Audio/_DeadSpace/Autosurgeon/sound_weapons_circsawhit.ogg");
}

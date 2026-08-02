// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.ThermalVision;

[RegisterComponent, NetworkedComponent]
public sealed partial class ThermalVisorComponent : Component
{
    [DataField]
    public Color? Color = null;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool HasThermalVision = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Animation = true;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool UseShader = true;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? ActivateSound = null;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? ActivateSoundOff = null;
}
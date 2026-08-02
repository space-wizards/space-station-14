// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.ThermalVision;

[RegisterComponent, NetworkedComponent]
public sealed partial class ThermalVisorExperimentalComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool HasThermalVision = false;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float PulseDuration = 2f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool UseShader = true;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ActivateSound = null;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ActivateSoundOff = null;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? LastToggleTime;
}
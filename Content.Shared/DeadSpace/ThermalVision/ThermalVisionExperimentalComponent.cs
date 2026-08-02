// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.ThermalVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ThermalVisionExperimentalComponent : Component
{
    [DataField]
    public EntProtoId ActionToggleThermalVisionExperimental = "ActionToggleThermalVisionExperimental";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionToggleThermalVisionExperimentalEntity;

    [DataField, AutoNetworkedField]
    public bool IsActive;

    [DataField, AutoNetworkedField]
    public float PulseDuration = 2f;

    [DataField, AutoNetworkedField]
    public float CurrentPulseTime;

    [DataField, AutoNetworkedField]
    public bool UseShader = true;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ActivateSound = null;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ActivateSoundOff = null;

    [DataField, AutoNetworkedField]
    public EntityUid? VisorUid;

    [DataField, AutoNetworkedField]
    public TimeSpan? LastToggleTime;
}

public sealed partial class ToggleThermalVisionExperimentalActionEvent : InstantActionEvent;
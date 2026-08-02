// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.ThermalVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ThermalVisionComponent : Component
{
    [DataField]
    public EntProtoId ActionToggleThermalVision = "ActionToggleThermalVision";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionToggleThermalVisionEntity;

    [DataField, AutoNetworkedField]
    public bool IsActive;

    [DataField, AutoNetworkedField]
    public bool Animation = true;

    [DataField, AutoNetworkedField]
    public bool UseShader = true;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ActivateSound = null;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ActivateSoundOff = null;
}

public sealed partial class ToggleThermalVisionActionEvent : InstantActionEvent;
using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.NightVision;

[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("isNightVision")]
    public bool IsNightVision;

    /// <description>
    /// Used to ensure that this doesn't break with sandbox or admin tools.
    /// This is not "enabled/disabled".
    /// </description>
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool LightSetup = false;

    /// <description>
    /// Gives an extra frame of nighyvision to reenable light manager during
    /// </description>
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool GraceFrame = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color Color = Color.White;
}

public sealed partial class ToggleNightVisionActionEvent : InstantActionEvent { }

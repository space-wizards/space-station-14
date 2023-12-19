using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BlindableSystem))]
public sealed partial class BlindableComponent : Component
{
    /// <summary>
    /// How many seconds will be subtracted from each attempt to add blindness to us?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("isBlind"), AutoNetworkedField]
    public bool IsBlind;

    /// <summary>
    /// Eye damage due to things like staring directly at welders. Causes blurry vision or outright
    /// blindness if greater than or equal to <see cref="MaxDamage"/>.
    /// </summary>
    /// <remarks>
    /// Should eventually be replaced with a proper eye health system when we have bobby.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), DataField("EyeDamage"), AutoNetworkedField]
    public int EyeDamage = 0;

    public const int MaxDamage = 3;

    /// <description>
    /// Used to ensure that this doesn't break with sandbox or admin tools.
    /// This is not "enabled/disabled".
    /// </description>
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool LightSetup = false;

    /// <description>
    /// Gives an extra frame of blindness to reenable light manager during
    /// </description>
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool GraceFrame = false;
}

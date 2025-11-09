
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Components.NightVision;

[RegisterComponent]
public sealed partial class PNVComponent : Component
{
    [DataField]
    public Color? Color = null;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool HasNightVision = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? ActivateSound = null;
}

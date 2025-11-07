using Content.Shared.DeadSpace.NightVision;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Components.NightVision;

[RegisterComponent]
public sealed partial class NightVisionComponent : SharedNightVisionComponent
{
    [DataField]
    public EntProtoId ActionToggleNightVision = "ActionToggleNightVision";

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public EntityUid? ActionToggleNightVisionEntity;

    public NightVisionComponent(Color? color = null, SoundSpecifier? activateSound = null)
    {
        Color = color ?? new Color(80f / 255f, 220f / 255f, 70f / 255f, 0.2f);
        ActivateSound = activateSound;
    }
}

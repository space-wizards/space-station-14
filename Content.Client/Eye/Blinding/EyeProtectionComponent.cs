using Content.Shared.Eye.Blinding.Components;
using Robust.Client.Graphics;

namespace Content.Client.Eye.Blinding;

/// <summary>
/// For welding masks, sunglasses, etc.
/// </summary>
[RegisterComponent]
public sealed class EyeProtectionComponent : SharedEyeProtectionComponent
{
    /// <summary>
    /// When worn, these glasses override nightvision as so
    /// </summary>
    [DataField("nightVision"), ViewVariables(VVAccess.ReadWrite)]
    public NightVision? Night;

    /// <summary>
    /// When worn, these glasses override autoexpose as so
    /// </summary>
    [DataField("autoExpose"), ViewVariables(VVAccess.ReadWrite)]
    public AutoExpose? AutoExpose;
}

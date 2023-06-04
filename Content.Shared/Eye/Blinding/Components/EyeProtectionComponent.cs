using Robust.Client.Graphics;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
/// For welding masks, sunglasses, etc.
/// </summary>
[RegisterComponent]
public sealed class EyeProtectionComponent : Component
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

    /// <summary>
    /// How many seconds to subtract from the status effect. If it's greater than the source
    /// of blindness, do not blind.
    /// </summary>
    [DataField("protectionTime")]
    public readonly TimeSpan ProtectionTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How much of your night vision wearing these costs you. Divides incoming light by this.
    /// </summary>
    [DataField("reduction")]
    public float Reduction = 1.0f;
}

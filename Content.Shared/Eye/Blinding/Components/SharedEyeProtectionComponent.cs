namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
/// For welding masks, sunglasses, etc.
/// </summary>
public partial class SharedEyeProtectionComponent : Component
{
    /// <summary>
    /// How many seconds to subtract from the status effect. If it's greater than the source
    /// of blindness, do not blind.
    /// </summary>
    [DataField("protectionTime")]
    public TimeSpan ProtectionTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How much of your night vision wearing these costs you. Divides incoming light by this.
    /// </summary>
    [DataField("reduction")]
    public float Reduction = 1.0f;
}

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
/// For welding masks, sunglasses, etc.
/// </summary>
[RegisterComponent]
public sealed class EyeProtectionComponent : Component
{
    /// <summary>
    /// How many seconds to subtract from the status effect. If it's greater than the source
    /// of blindness, do not blind.
    /// </summary>
    [DataField("protectionTime")]
    public readonly TimeSpan ProtectionTime = TimeSpan.FromSeconds(10);
}

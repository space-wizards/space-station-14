using Robust.Shared.Audio;

namespace Content.Server.Weapons.Ranged.Components;

/// <summary>
///     Responsible for handling recharging a basic entity ammo provider over time.
/// </summary>
[RegisterComponent]
public sealed class RechargeBasicEntityAmmoComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("minRechargeCooldown")]
    public float MinRechargeCooldown = 30f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxRechargeCooldown")]
    public float MaxRechargeCooldown = 45f;

    [DataField("rechargeSound")]
    public SoundSpecifier RechargeSound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg")
    {
        Params = AudioParams.Default.WithVolume(-5f)
    };

    [DataField("accumulatedFrametime")]
    public float AccumulatedFrameTime;
    /// <summary>
    ///     Number of seconds until the next recharge.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float NextRechargeTime = 0f;
}

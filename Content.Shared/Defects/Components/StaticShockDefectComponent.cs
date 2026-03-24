namespace Content.Shared.Defects.Components;

/// <summary>
/// Gives a handheld electronic item (emag, access breaker, etc.) a chance to
/// zap the user each time it is successfully used on a target.
/// Insulated gloves (combat gloves, captain's gloves, etc.) block the shock
/// automatically via the existing electrocution insulation system.
/// </summary>
[RegisterComponent]
public sealed partial class StaticShockDefectComponent : DefectComponent
{
    public StaticShockDefectComponent()
    {
        Prob = 0.20f;
        DefectLabel = "faulty discharge capacitor";
    }

    // Per-use probability of shocking the holder.
    [DataField]
    public float ShockChance = 0.50f;

    // Shock damage (in Shock damage type) applied to the user.
    [DataField]
    public int ShockDamage = 10;

    // How long the stun / jitter effect lasts.
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1.5);
}

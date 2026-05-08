namespace Content.Server._FinalStand.Economy;

/// <summary>
/// Optional per-enemy-prototype kill credit override.
/// If absent, WaveGameRuleComponent.KillReward is used as the fallback.
/// </summary>
[RegisterComponent]
public sealed partial class FSEnemyValueComponent : Component
{
    [DataField]
    public int KillCredits = 100;
}

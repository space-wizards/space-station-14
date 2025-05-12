namespace Content.Server._Impstation.Medical;

/// <summary>
/// Stuns the healed entity after a topical is applied.
/// </summary>
[RegisterComponent]
public sealed partial class StunOnHealComponent : Component
{
    /// <summary>
    /// How much shock damage should the stun do?
    /// </summary>
    [DataField]
    public int Damage = 1;

    /// <summary>
    /// How long should the topical stun?
    /// </summary>
    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(2);
}

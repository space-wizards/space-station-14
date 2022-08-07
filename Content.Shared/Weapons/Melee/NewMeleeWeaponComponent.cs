using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee;

[RegisterComponent, NetworkedComponent]
public sealed class NewMeleeWeaponComponent : Component
{
    // TODO: Can't use accumulator because we'd need an active component and client can't predict changing it.
    /// <summary>
    /// Next time this melee weapon can attack.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("nextAttack")]
    public TimeSpan NextAttack = TimeSpan.Zero;

    /// <summary>
    /// How much windup time have we accumulated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("windupAccumulator")]
    public float WindupAccumulator = 0f;

    /// <summary>
    /// How long it takes an attack to windup.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("windupTime")]
    public float WindupTime = 1f;

    /// <summary>
    /// Cooldown from ending one attack and starting another.
    /// </summary>
    // [ViewVariables(VVAccess.ReadWrite), DataField("cooldown")]
    // public TimeSpan Cooldown = TimeSpan.FromSeconds(0.5);

    // Sounds
    [ViewVariables(VVAccess.ReadWrite), DataField("soundMiss")]
    public SoundSpecifier? SoundMiss;
}

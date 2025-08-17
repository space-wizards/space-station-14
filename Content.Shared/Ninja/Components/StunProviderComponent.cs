using Content.Shared.Damage;
using Content.Shared.Ninja.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for stunning mobs on click outside of harm mode.
/// Knocks them down for a bit and deals shock damage.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedStunProviderSystem))]
public sealed partial class StunProviderComponent : Component
{
    /// <summary>
    /// The powercell entity to take power from.
    /// Determines whether stunning is possible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? BatteryUid;

    /// <summary>
    /// Sound played when stunning someone.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("sparks");

    /// <summary>
    /// Joules required in the battery to stun someone. Defaults to 10 uses on a small battery.
    /// </summary>
    [DataField]
    public float StunCharge = 36f;

    /// <summary>
    /// Damage dealt when stunning someone
    /// </summary>
    [DataField]
    public DamageSpecifier StunDamage = new()
    {
        DamageDict = new()
        {
            { "Shock", 5 }
        }
    };

    /// <summary>
    /// Time that someone is stunned for, stacks if done multiple times.
    /// </summary>
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long stunning is disabled after stunning something.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2);

    /// <summary>
    /// ID of the cooldown use delay.
    /// </summary>
    [DataField]
    public string DelayId = "stun_cooldown";

    /// <summary>
    /// Locale string to popup when there is no power
    /// </summary>
    [DataField(required: true)]
    public LocId NoPowerPopup = string.Empty;

    /// <summary>
    /// Whitelist for what counts as a mob.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();
}

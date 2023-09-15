using Content.Shared.Ninja.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for stunning mobs on click outside of harm mode.
/// Knocks them down for a bit and deals shock damage.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStunProviderSystem))]
public sealed partial class StunProviderComponent : Component
{
    /// <summary>
    /// The powercell entity to take power from.
    /// Determines whether stunning is possible.
    /// </summary>
    [DataField("batteryUid"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? BatteryUid;

    /// <summary>
    /// Sound played when stunning someone.
    /// </summary>
    [DataField("sound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("sparks");

    /// <summary>
    /// Joules required in the battery to stun someone. Defaults to 10 uses on a small battery.
    /// </summary>
    [DataField("stunCharge"), ViewVariables(VVAccess.ReadWrite)]
    public float StunCharge = 36.0f;

    /// <summary>
    /// Shock damage dealt when stunning someone
    /// </summary>
    [DataField("stunDamage"), ViewVariables(VVAccess.ReadWrite)]
    public int StunDamage = 5;

    /// <summary>
    /// Time that someone is stunned for, stacks if done multiple times.
    /// </summary>
    [DataField("stunTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StunTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long stunning is disabled after stunning something.
    /// </summary>
    [DataField("cooldown"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Locale string to popup when there is no power
    /// </summary>
    [DataField("noPowerPopup", required: true), ViewVariables(VVAccess.ReadWrite)]
    public string NoPowerPopup = string.Empty;

    /// <summary>
    /// Whitelist for what counts as a mob.
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist Whitelist = new()
    {
        Components = new[] {"Stamina"}
    };

    /// <summary>
    /// When someone can next be stunned.
    /// Essentially a UseDelay unique to this component.
    /// </summary>
    [DataField("nextStun", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextStun = TimeSpan.Zero;
}

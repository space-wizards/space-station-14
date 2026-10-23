using Content.Shared.Changeling.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Component responsible for Changelings Regenerative Stasis.
/// Allows the user to fake "death" and heal afterwards.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RegenerativeStasisSystem))]
public sealed partial class RegenerativeStasisActionComponent : Component
{
    // TODO: Will need small behaviour tweaks once we get biomass/chemicals.

    /// <summary>
    /// Whether this entity is currently in stasis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsInStasis;

    /// <summary>
    /// Whether the entity can ghost out during their stasis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowGhosting;

    /// <summary>
    /// Minimum time the entity has to be in stasis before they are allowed to get up.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MinStasisCooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum time the entity has to be in stasis before they are allowed to get up, based on the entity's sustained damage and CooldownBonusPerDamage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MaxStasisCooldown = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The amount of bonus cooldown time to add on top of <see cref="MinStasisCooldown"/> for each unit of damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan BonusCooldownPerDamage = TimeSpan.FromSeconds(30d / 200d); // at 200 damage, the cooldown should be 60s

    /// <summary>
    /// The sound to play when the entity exits stasis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ExitSound = new SoundPathSpecifier("/Audio/Magic/rumble.ogg");

    /// <summary>
    /// The name this entity's action started with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? InitialName;

    /// <summary>
    /// The description this entity's action started with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? InitialDescription;

    /// <summary>
    /// Name that the action changes to when in stasis
    /// </summary>
    [DataField]
    public LocId ActiveName = "changeling-stasis-active-name";

    /// <summary>
    /// Description of the action while in stasis
    /// </summary>
    [DataField]
    public LocId ActiveDescription= "changeling-stasis-active-desc";

    /// <summary>
    /// Popup to display to the action holder when entering stasis
    /// </summary>
    [DataField]
    public LocId EnterStasisPopup = "changeling-stasis-enter";

    /// <summary>
    /// Popup to display to the action holder when exiting stasis
    /// </summary>
    [DataField]
    public LocId ExitStasisPopup = "changeling-stasis-exit";

    /// <summary>
    /// Popup to display to other entities when exiting stasis
    /// </summary>
    [DataField]
    public LocId ExitStasisPopupOthers = "changeling-stasis-exit-others";
    }

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Magic.Systems;

namespace Content.Shared.Magic.Components;

/// <summary>
/// Component for items that grant a world-target projectile action when used.
/// The action will raise a <see cref="Content.Shared.Magic.Events.ProjectileSpellEvent"/>, which
/// the magic system handles to spawn and shoot the projectile.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MagicItemSystem))]
public sealed partial class MagicItemComponent : Component
{
    /// <summary>
    /// Action prototype to create on map init. This action should use a world-target event
    /// (e.g. a prototype similar to those in `Resources/Prototypes/Magic/projectile_spells.yml`).
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Action = string.Empty;

    /// <summary>
    /// Projectile prototype to fire. This will be assigned to the spawned action's
    /// world-target event (ProjectileSpellEvent.prototype) so the spell system spawns it.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Projectile = string.Empty;

    /// <summary>
    /// Optional cooldown (seconds). If set to > 0 this will override the action's UseDelay.
    /// If left at 0, the action's prototype UseDelay is used.
    /// </summary>
    [DataField]
    public float UseDelaySeconds = 0f;

    /// <summary>
    /// The action entity created by the actions system.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ActionEntity;
}

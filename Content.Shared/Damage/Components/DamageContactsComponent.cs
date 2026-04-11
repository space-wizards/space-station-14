using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Causes continuous damage to entities colliding with this entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DamageContactsComponent : Component
{
    /// <summary>
    /// The damage done each second to those touching this entity.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Entities that aren't damaged by this entity.
    /// </summary>
    [DataField]
    public EntityWhitelist? IgnoreWhitelist;
}

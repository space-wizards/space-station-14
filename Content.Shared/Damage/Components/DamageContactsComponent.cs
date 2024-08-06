using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[NetworkedComponent, RegisterComponent]
public sealed partial class DamageContactsComponent : Component
{
    /// <summary>
    /// The damage done each second to those touching this entity
    /// </summary>
    [DataField("damage", required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Entities that aren't damaged by this entity
    /// </summary>
    [DataField("ignoreWhitelist")]
    public EntityWhitelist? IgnoreWhitelist;
}

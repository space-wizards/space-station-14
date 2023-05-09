using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Marker;

/// <summary>
/// Applies <see cref="DamageMarkerComponent"/> when colliding with an entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class DamageMarkerOnCollideComponent : Component
{
    [DataField("whitelist"), AutoNetworkedField]
    public EntityWhitelist? Whitelist = new();

    [DataField("duration")]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Additional damage to be applied.
    /// </summary>
    [DataField("damage")]
    public DamageSpecifier Damage = new();
}

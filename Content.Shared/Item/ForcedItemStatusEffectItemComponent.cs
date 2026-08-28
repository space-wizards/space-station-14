using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// Marker component for the items spawned by <see cref="ForcedItemStatusEffectSystem"/>.
/// For mutual cleanup.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ForcedItemStatusEffectItemComponent : Component
{
    /// <summary>
    /// The status effect that forced this item to spawn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? StatusEffect;
}

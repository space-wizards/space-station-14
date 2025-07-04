using Content.Shared.Storage;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Indicates that the entity can be butchered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ButcherableComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField("spawned", required: true), AutoNetworkedField]
    public List<EntitySpawnEntry> SpawnedEntities = [];

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ButcherDelay = 8.0f;

    /// <summary>
    ///
    /// </summary>
    [DataField("butcheringType"), AutoNetworkedField]
    public ButcheringType Type = ButcheringType.Knife;
}

/// <summary>
///
/// </summary>
public enum ButcheringType : byte
{
    Knife, // e.g. goliaths
    Spike, // e.g. monkeys
    Gibber // e.g. humans. TODO
}

using Content.Shared.Storage;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Indicates that the entity can be butchered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ButcherableComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField("spawned", required: true)]
    public List<EntitySpawnEntry> SpawnedEntities = [];

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public float ButcherDelay = 8.0f;

    /// <summary>
    ///
    /// </summary>
    [DataField("butcheringType")]
    public ButcheringType Type = ButcheringType.Knife;

    /// <summary>
    /// Prevents butchering same entity on two and more spikes simultaneously and multiple doAfters on the same Spike
    /// </summary>
    [ViewVariables]
    public bool BeingButchered;
}

public enum ButcheringType : byte
{
    Knife, // e.g. goliaths
    Spike, // e.g. monkeys
    Gibber // e.g. humans. TODO
}

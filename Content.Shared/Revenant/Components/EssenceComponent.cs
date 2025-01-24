using System.Numerics;
using Content.Shared.Mobs;
using Content.Shared.Revenant.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedEssenceSystem), typeof(SharedRevenantSystem))]
public sealed partial class EssenceComponent : Component
{
    /// <summary>
    ///     Whether the entity has been harvested yet.
    /// </summary>
    [DataField]
    public bool Harvested;

    /// <summary>
    ///     Whether a revenant has searched this entity for its soul yet.
    /// </summary>
    [DataField]
    public bool SearchComplete;

    /// <summary>
    ///     The total amount of Essence that the entity has. Changes based on mob state.
    /// </summary>
    [DataField]
    public float EssenceAmount;

    /// <summary>
    ///     The essence range for different mob states when the entity has no mind.
    /// </summary>
    [DataField]
    public Dictionary<MobState, Vector2> MindlessEssenceRanges = new()
    {
        { MobState.Alive, new Vector2(45f, 70f) },
        { MobState.Critical, new Vector2(35f, 50f) },
        { MobState.Dead, new Vector2(15f, 20f) },
    };

    /// <summary>
    ///     The essence range for different mob states when the entity has a mind.
    /// </summary>
    [DataField]
    public Dictionary<MobState, Vector2> MindfulEssenceRanges = new()
    {
        { MobState.Alive, new Vector2(75f, 100f) },
        { MobState.Critical, new Vector2(35f, 50f) },
        { MobState.Dead, new Vector2(15f, 20f) },
    };
}

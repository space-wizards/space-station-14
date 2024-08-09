using Content.Server.Administration.Systems;
using Content.Shared.Climbing.Components;

namespace Content.Server.Administration.Components;

/// <summary>
/// Component to track the timer for the SuperBonk smite.
/// </summary>
[RegisterComponent, Access(typeof(SuperBonkSystem))]
public sealed partial class SuperBonkComponent: Component
{
    /// <summary>
    /// Entity being Super Bonked.
    /// </summary>
    [DataField]
    public EntityUid Target;

    /// <summary>
    /// All of the tables the target will be bonked on.
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, BonkableComponent>.Enumerator Tables;

    /// <summary>
    /// Value used to reset the timer once it expires.
    /// </summary>
    [DataField]
    public float InitialTime = 0.10f;

    /// <summary>
    /// Timer till the next bonk.
    /// </summary>
    [DataField]
    public float TimeRemaining = 0.10f;

    /// <summary>
    /// Whether to remove the clumsy component from the target after SuperBonk is done.
    /// </summary>
    [DataField]
    public bool RemoveClumsy = true;

    /// <summary>
    /// Whether to stop Super Bonk on the target once he dies. Otherwise it will continue until no other tables are left
    /// or the target is gibbed.
    /// </summary>
    [DataField]
    public bool StopWhenDead = true;
}

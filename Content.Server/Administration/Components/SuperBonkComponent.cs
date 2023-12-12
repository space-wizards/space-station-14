using Content.Shared.Climbing.Components;

namespace Content.Server.Administration.Components;

/// <summary>
/// Component to track the timer for the SuperBonk smite.
/// </summary>
[RegisterComponent]
public sealed partial class SuperBonkComponent: Component
{
    [DataField("target")]
    public EntityUid Target;

    [DataField("tables")]
    public Dictionary<EntityUid, BonkableComponent>.Enumerator Tables;

    [DataField("initialTime")]
    public float InitialTime = 0.10f;

    [DataField("timeRemaining")]
    public float TimeRemaining = 0.10f;

    [DataField("removeClumsy")]
    public bool RemoveClumsy = true;

    [DataField("stopWhenDead")]
    public bool StopWhenDead = true;
}

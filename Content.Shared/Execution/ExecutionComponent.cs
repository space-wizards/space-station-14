using Robust.Shared.GameStates;

namespace Content.Shared.Execution;

/// <summary>
/// Added to entities that can be used to execute another target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExecutionComponent : Component
{
    /// <summary>
    /// How long the execution duration lasts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DoAfterDuration = 5f;

    [DataField, AutoNetworkedField]
    public float DamageModifier = 9f;

    [DataField]
    public LocId DefaultInternalMeleeExecutionMessage = "execution-popup-melee-initial-internal";

    [DataField]
    public LocId DefaultExternalMeleeExecutionMessage = "execution-popup-melee-initial-external";

    [DataField]
    public LocId DefaultCompleteInternalMeleeExecutionMessage = "execution-popup-melee-complete-internal";

    [DataField]
    public LocId DefaultCompleteExternalMeleeExecutionMessage = "execution-popup-melee-complete-external";

    // Not networked because this is transient inside of a tick.
    /// <summary>
    /// True if it is currently executing for handlers.
    /// </summary>
    [DataField]
    public bool Executing = false;
}

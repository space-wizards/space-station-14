using Robust.Shared.GameStates;

namespace Content.Shared.Execution;

/// <summary>
/// Added to entities that can be used to execute another target.
/// </summary>
[RegisterComponent]
public sealed partial class ExecutionComponent : Component
{
    /// <summary>
    /// How long the melee execution duration lasts.
    /// </summary>
    public static float MeleeDoAfterDuration = 5f;
    
    /// <summary>
    /// How long the gun execution duration lasts.
    /// </summary>
    public static float GunDoAfterDuration = 6f;

    /// <summary>
    /// Arbitrarily chosen number to multiply damage by, used to deal reasonable amounts of damage to a victim of an execution.
    /// /// </summary>
    public static float DamageMultiplier = 9f;

    /// <summary>
    /// Shown to the person performing the melee execution (attacker) upon starting a melee execution.
    /// </summary>
    public static LocId InternalMeleeExecutionMessage = "execution-popup-melee-initial-internal";

    public static LocId InternalGunExecutionMessage = "execution-popup-gun-initial-internal";

    /// <summary>
    /// Shown to bystanders and the victim of a melee execution when a melee execution is started.
    /// </summary>

    public static LocId ExternalMeleeExecutionMessage = "execution-popup-melee-initial-external";

    public static LocId ExternalGunExecutionMessage = "execution-popup-gun-initial-external";

    /// <summary>
    /// Shown to the attacker upon completion of a melee execution.
    /// </summary>

    public static LocId CompleteInternalMeleeExecutionMessage = "execution-popup-melee-complete-internal";

    public static LocId CompleteInternalGunExecutionMessage = "execution-popup-gun-complete-internal";

    /// <summary>
    /// Shown to bystanders and the victim of a melee execution when a melee execution is completed.
    /// </summary>

    public static LocId CompleteExternalMeleeExecutionMessage = "execution-popup-melee-complete-external";

    public static LocId CompleteExternalGunExecutionMessage = "execution-popup-gun-complete-external";

    /// <summary>
    /// Shown to the person performing the self execution when starting one.
    /// </summary>

    public static LocId InternalSelfMeleeExecutionMessage = "execution-popup-self-melee-initial-internal";

    public static LocId InternalSelfGunExecutionMessage = "execution-popup-self-gun-initial-internal";

    /// <summary>
    /// Shown to bystanders near a self execution when one is started.
    /// </summary>

    public static LocId ExternalSelfMeleeExecutionMessage = "execution-popup-self-melee-initial-external";

    public static LocId ExternalSelfGunExecutionMessage = "execution-popup-self-gun-initial-external";

    /// <summary>
    /// Shown to the person performing a self execution upon completion of a do-after or on use of /suicide with a weapon that has the Execution component.
    /// </summary>

    public static LocId CompleteInternalSelfMeleeExecutionMessage = "execution-popup-self-melee-complete-internal";

    public static LocId CompleteInternalSelfGunExecutionMessage = "execution-popup-self-gun-complete-internal";

    /// <summary>
    /// Shown to bystanders when a self execution is completed or a suicide via execution weapon happens nearby.
    /// </summary>

    public static LocId CompleteExternalSelfMeleeExecutionMessage = "execution-popup-self-melee-complete-external";

    public static LocId CompleteExternalSelfGunExecutionMessage = "execution-popup-self-gun-complete-external";
}

using Content.Shared.Trigger.Systems;

namespace Content.Shared.Trigger.Components.StepTriggers;

/// <inheritdoc cref="Triggers.BaseTriggerOnXComponent"/>
public abstract partial class BaseStepTriggerOnXComponent : Component
{
    /// <inheritdoc cref="Triggers.BaseTriggerOnXComponent.KeyOut"/>
    [DataField, AutoNetworkedField]
    public string? KeyOut = TriggerSystem.DefaultTriggerKey;

    /// <summary>
    /// If true, the trigger attempt will be cancelled if doesn't face conditions,
    /// instead of just continue. By default, it's off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Cancellable = false;
}

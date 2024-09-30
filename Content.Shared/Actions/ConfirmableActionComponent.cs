using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Actions;

/// <summary>
/// An action that must be confirmed before using it.
/// Using it for the first time primes it, after a delay you can then confirm it.
/// Used for dangerous actions that cannot be undone (unlike screaming).
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ConfirmableActionSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ConfirmableActionComponent : Component
{
    /// <summary>
    /// Warning popup shown when priming the action.
    /// </summary>
    [DataField(required: true)]
    public LocId Popup = string.Empty;

    /// <summary>
    /// If not null, this is when the action can be confirmed at.
    /// This is the time of priming plus the delay.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextConfirm;

    /// <summary>
    /// If not null, this is when the action will unprime at.
    /// This is <c>NextConfirm> plus <c>PrimeTime</c>
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextUnprime;

    /// <summary>
    /// Forced delay between priming and confirming to prevent accidents.
    /// </summary>
    [DataField]
    public TimeSpan ConfirmDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Once you prime the action it will unprime after this length of time.
    /// </summary>
    [DataField]
    public TimeSpan PrimeTime = TimeSpan.FromSeconds(5);
}

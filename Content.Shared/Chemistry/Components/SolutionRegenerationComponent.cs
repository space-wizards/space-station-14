using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Passively increases a solution's quantity of a reagent.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, AutoGenerateComponentState, NetworkedComponent]
[Access(typeof(SolutionRegenerationSystem))]
public sealed partial class SolutionRegenerationComponent : Component
{
    /// <summary>
    /// The reagent(s) to be regenerated in the solution.
    /// </summary>
    [DataField(required: true)]
    public Solution Generated = default!;

    /// <summary>
    /// How long it takes to regenerate once.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The time when the next regeneration will occur.
    /// </summary>
    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextRegenTime;
}

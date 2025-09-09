namespace Content.Shared.Bed.Components;

/// <summary>
///     Entities in the critical state strapped to this bed will receive stabilizing effects.
/// </summary>
[RegisterComponent]
public sealed partial class StabilizeOnBuckleComponent : Component
{
    /// <summary>
    ///     How much Asphyxiation and Bloodloss are prevented in the critical state.
    ///     1 = All damage prevented, 0 = no stabilization.
    /// </summary>
    [DataField(required: true)]
    public float Efficiency;

    /// <summary>
    ///     How much bleeding it stops.
    /// </summary>
    [DataField]
    public float ReducesBleeding = 0f;
}


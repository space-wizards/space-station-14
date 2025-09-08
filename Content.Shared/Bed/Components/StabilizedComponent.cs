namespace Content.Shared.Bed.Components;

[RegisterComponent]
public sealed partial class StabilizedComponent : Component
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

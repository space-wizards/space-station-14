namespace Content.Shared.Ninja.Components
{
    /// <summary>
    /// Makes this warp point a valid bombing target for ninja's spider charge.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BombingTargetComponent : Component
    {
        /// <summary>
        /// Imp addition. Text used in the ninja's objectives for the location of the bombing target.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public string? Location;
    }
}

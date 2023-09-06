using System.Threading;
namespace Content.Server.Body.Components
{
    /// <summary>
    /// Handles hooking up a mask (breathing tool) / gas tank together and allowing the Owner to breathe through it.
    /// </summary>
    [RegisterComponent]
    public sealed partial class InternalsComponent : Component
    {
        [ViewVariables] public EntityUid? GasTankEntity { get; set; }
        [ViewVariables] public EntityUid? BreathToolEntity { get; set; }

        /// <summary>
        /// Toggle Internals delay (seconds) when the target is not you.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("delay")]
        public float Delay = 3;
    }
}

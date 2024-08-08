using Robust.Shared.Prototypes;

namespace Content.Server.Medical.Stethoscope.Components
{
    /// <summary>
    /// Adds an innate verb when equipped to use a stethoscope.
    /// </summary>
    [RegisterComponent]
    public sealed partial class StethoscopeComponent : Component
    {
        public bool IsActive = false;

        [DataField("delay")]
        public float Delay = 2.5f;

        [DataField]
        public EntProtoId Action = "ActionStethoscope";

        [DataField("actionEntity")] public EntityUid? ActionEntity;
    }
}

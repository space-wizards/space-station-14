using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Body.Surgery.Operation.Step
{
    [Prototype("surgeryStep")]
    public class SurgeryStepPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = string.Empty;

        public string LocId => ID.ToLowerInvariant();
    }
}

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Body.Metabolism
{
    [Prototype("metabolismType")]
    public class MetabolismTypePrototype : IPrototype
    {
        [DataField("id", required: true)]
        public string ID => default!;
    }
}

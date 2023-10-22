using Robust.Shared.Prototypes;

namespace Content.Shared.Dataset
{
    [Prototype("dataset")]
    public sealed class DatasetPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("values")] public IReadOnlyList<string> Values { get; private set; } = new List<string>();
    }
}

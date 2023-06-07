using Robust.Shared.Prototypes;

namespace Content.Server.ErtCall
{
    [Serializable, Prototype("ertCall")]
    public sealed class ErtCallPresetPrototype : IPrototype
    {
        [IdDataFieldAttribute] public string ID { get; } = default!;

        [DataField("path")] public string path { get; set; } = string.Empty;
    }
}

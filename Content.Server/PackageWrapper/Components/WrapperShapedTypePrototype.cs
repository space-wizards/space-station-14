using Robust.Shared.Prototypes;

namespace Content.Server.PackageWrapper.Components
{
    [Serializable, Prototype("WrapShapedType")]
    public class WrapperShapedTypePrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("protospawnid")]
        public string ProtoSpawnID { get; } = string.Empty;
    }
}

using Robust.Shared.Prototypes;

namespace Content.Server.PackageWrapper.Components
{
    [Prototype("WrapShapedType")]
    public class WrapperShapedTypePrototype : IPrototype
    {
        [IdDataFieldAttribute] public string ID { get; } = null!;

        [DataField("protospawnid")] public string ProtoSpawnID { get; } = null!;
    }
}

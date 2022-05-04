using Robust.Shared.Prototypes;

namespace Content.Server.PackageWrapper.Components
{
    [Prototype("WrapType")]
    public class WrapperTypePrototype : IPrototype
    {
        [IdDataFieldAttribute] public string ID { get; } = null!;

        [DataField("protospawnid")] public string ProtoSpawnID { get; } = null!;

        [DataField("capacity")] public int Capacity { get; } = 0;
    }
}

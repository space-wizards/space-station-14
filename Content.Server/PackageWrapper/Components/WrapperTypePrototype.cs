using Robust.Shared.Prototypes;

namespace Content.Server.PackageWrapper.Components
{
    [Serializable, Prototype("WrapType")]
    public class WrapperTypePrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("protospawnid")]
        public string ProtoSpawnID { get; } = string.Empty;

        [DataField("capacity")]
        public int Capacity = 0;
    }
}

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.BarSign
{
    [Prototype("barSign")]
    public readonly struct BarSignPrototype : IPrototype
    {
        private readonly string _description = string.Empty;
        private readonly string _name = string.Empty;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;


        [DataField("icon")] public string Icon { get; } = string.Empty;

        [DataField("name", customTypeSerializer: typeof(LocStringSerializer))]
        public readonly string Name;

        [DataField("description", customTypeSerializer: typeof(LocStringSerializer))]
        public readonly string Description;

        [DataField("renameArea")] public bool RenameArea { get; } = true;

        [DataField("hidden")] public bool Hidden { get; }
    }
}

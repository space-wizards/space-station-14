using Robust.Shared.Prototypes;

namespace Content.Shared.BarSign
{
    [Prototype("barSign")]
    public sealed partial class BarSignPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;


        [DataField("icon")] public string Icon { get; private set; } = string.Empty;

        [DataField("name")] public string Name { get; set; } = "";
        [DataField("description")] public string Description { get; set; } = "";

        [DataField("renameArea")]
        public bool RenameArea { get; private set; } = true;
        [DataField("hidden")]
        public bool Hidden { get; private set; }
    }
}

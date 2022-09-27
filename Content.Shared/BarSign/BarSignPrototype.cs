using Robust.Shared.Prototypes;

namespace Content.Shared.BarSign
{
    [Prototype("barSign")]
    public sealed class BarSignPrototype : IPrototype
    {
        private string _description = string.Empty;
        private string _name = string.Empty;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;


        [DataField("icon")] public string Icon { get; private set; } = string.Empty;

        [DataField("name")]
        public string Name
        {
            get => _name;
            private set => _name = Loc.GetString(value);
        }

        [DataField("description")]
        public string Description
        {
            get => _description;
            private set => _description = Loc.GetString(value);
        }

        [DataField("renameArea")]
        public bool RenameArea { get; private set; } = true;
        [DataField("hidden")]
        public bool Hidden { get; private set; }
    }
}

using Robust.Shared.Prototypes;

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

        [DataField("renameArea")] public bool RenameArea { get; } = true;

        [DataField("hidden")] public bool Hidden { get; }
    }
}

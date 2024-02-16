using Content.Shared.Construction.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Construction.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class MachineBoardComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [DataField("requirements", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, MachinePartPrototype>))]
        public Dictionary<string, int> Requirements = new();

        [DataField("materialRequirements")]
        public Dictionary<string, int> MaterialIdRequirements = new();

        [DataField("tagRequirements")]
        public Dictionary<string, GenericPartInfo> TagRequirements = new();

        [DataField("componentRequirements")]
        public Dictionary<string, GenericPartInfo> ComponentRequirements = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototype")]
        public string? Prototype { get; private set; }

        public IEnumerable<KeyValuePair<StackPrototype, int>> MaterialRequirements
        {
            get
            {
                foreach (var (materialId, amount) in MaterialIdRequirements)
                {
                    var material = _prototypeManager.Index<StackPrototype>(materialId);
                    yield return new KeyValuePair<StackPrototype, int>(material, amount);
                }
            }
        }
    }

    [Serializable]
    [DataDefinition]
    public partial struct GenericPartInfo
    {
        [DataField("Amount")]
        public int Amount;
        [DataField("ExamineName")]
        public string ExamineName;
        [DataField("DefaultPrototype")]
        public string DefaultPrototype;
    }
}

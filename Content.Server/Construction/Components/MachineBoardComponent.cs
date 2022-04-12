using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Components
{
    [RegisterComponent]
    public sealed class MachineBoardComponent : Component
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables]
        [DataField("requirements")]
        public readonly Dictionary<MachinePart, int> Requirements = new();

        [ViewVariables]
        [DataField("materialRequirements")]
        public readonly Dictionary<string, int> MaterialIdRequirements = new();

        [ViewVariables]
        [DataField("tagRequirements")]
        public readonly Dictionary<string, GenericPartInfo> TagRequirements = new();

        [ViewVariables]
        [DataField("componentRequirements")]
        public readonly Dictionary<string, GenericPartInfo> ComponentRequirements = new();

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
    public struct GenericPartInfo
    {
        [DataField("Amount")]
        public int Amount;
        [DataField("ExamineName")]
        public string ExamineName;
        [DataField("DefaultPrototype")]
        public string DefaultPrototype;
    }
}

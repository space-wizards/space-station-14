
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Prototypes;

[NetSerializable, Serializable, Prototype("medipenRecipe")]
public sealed partial class MedipenRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    private string _name = string.Empty;

    [DataField("description")]
    private string _description = string.Empty;

    [DataField("result", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Result = string.Empty;

    [DataField("icon")]
    public SpriteSpecifier? Icon;

    [DataField("reagents", required: true)]
    private List<ReagentQuantity> _requiredReagents = new();

    [ViewVariables]
    public string Name
    {
        get
        {
            if (_name.Trim().Length != 0) return _name;
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            protoMan.TryIndex(Result, out EntityPrototype? prototype);
            if (prototype?.Name != null)
                _name = prototype.Name;
            return _name;
        }
    }

    [ViewVariables]
    public string Description
    {
        get
        {
            if (_description.Trim().Length != 0) return _description;
            var protoMan = IoCManager.Resolve<IPrototypeManager>();
            protoMan.TryIndex(Result, out EntityPrototype? prototype);
            if (prototype?.Description != null)
                _description = prototype.Description;
            return _description;
        }
    }

    [ViewVariables]
    public List<ReagentQuantity> RequiredReagents
    {
        get => _requiredReagents;
        private set => _requiredReagents = value;
    }

}

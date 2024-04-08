using Content.Shared.Chemistry.Components;
namespace Content.Shared.ReagentOnItem;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ReagentOnItemComponent : Component
{
    [DataField("amountOfReagentLeft"), ViewVariables(VVAccess.ReadWrite)]
    public Double AmountOfReagentLeft;
}


